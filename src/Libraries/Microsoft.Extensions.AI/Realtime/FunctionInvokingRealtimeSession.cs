// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable S2219 // Runtime type checking should be simplified
#pragma warning disable S3353 // Unchanged local variables should be "const"

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating realtime session that invokes functions defined on <see cref="RealtimeClientResponseCreateMessage"/>.
/// Include this in a realtime session pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// <para>
/// When this session receives a <see cref="FunctionCallContent"/> in a realtime server message from its inner
/// <see cref="IRealtimeSession"/>, it responds by invoking the corresponding <see cref="AIFunction"/> defined
/// in <see cref="RealtimeClientResponseCreateMessage.Tools"/> (or in <see cref="AdditionalTools"/>), producing a <see cref="FunctionResultContent"/>
/// that it sends back to the inner session. This loop is repeated until there are no more function calls to make, or until
/// another stop condition is met, such as hitting <see cref="MaximumIterationsPerRequest"/>.
/// </para>
/// <para>
/// If a requested function is an <see cref="AIFunctionDeclaration"/> but not an <see cref="AIFunction"/>, the
/// <see cref="FunctionInvokingRealtimeSession"/> will not attempt to invoke it, and instead allow that <see cref="FunctionCallContent"/>
/// to pass back out to the caller. It is then that caller's responsibility to create the appropriate <see cref="FunctionResultContent"/>
/// for that call and send it back as part of a subsequent request.
/// </para>
/// <para>
/// A <see cref="FunctionInvokingRealtimeSession"/> instance is thread-safe for concurrent use so long as the
/// <see cref="AIFunction"/> instances employed as part of the supplied <see cref="RealtimeClientResponseCreateMessage"/> are also safe.
/// The <see cref="AllowConcurrentInvocation"/> property can be used to control whether multiple function invocation
/// requests as part of the same request are invocable concurrently, but even with that set to <see langword="false"/>
/// (the default), multiple concurrent requests to this same instance and using the same tools could result in those
/// tools being used concurrently (one per request).
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public class FunctionInvokingRealtimeSession : DelegatingRealtimeSession
{
    /// <summary>The <see cref="FunctionInvocationContext"/> for the current function invocation.</summary>
    private static readonly AsyncLocal<FunctionInvocationContext?> _currentContext = new();

    /// <summary>Gets the <see cref="IServiceProvider"/> specified when constructing the <see cref="FunctionInvokingRealtimeSession"/>, if any.</summary>
    protected IServiceProvider? FunctionInvocationServices { get; }

    /// <summary>The logger to use for logging information about function invocation.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="ActivitySource"/> to use for telemetry.</summary>
    /// <remarks>This component does not own the instance and should not dispose it.</remarks>
    private readonly ActivitySource? _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingRealtimeSession"/> class.
    /// </summary>
    /// <param name="innerSession">The underlying <see cref="IRealtimeSession"/>, or the next instance in a chain of sessions.</param>
    /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> to use for logging information about function invocation.</param>
    /// <param name="functionInvocationServices">An optional <see cref="IServiceProvider"/> to use for resolving services required by the <see cref="AIFunction"/> instances being invoked.</param>
    public FunctionInvokingRealtimeSession(IRealtimeSession innerSession, ILoggerFactory? loggerFactory = null, IServiceProvider? functionInvocationServices = null)
        : base(innerSession)
    {
        _logger = (ILogger?)loggerFactory?.CreateLogger<FunctionInvokingRealtimeSession>() ?? NullLogger.Instance;
        _activitySource = innerSession.GetService<ActivitySource>();
        FunctionInvocationServices = functionInvocationServices;
    }

    /// <summary>Gets the function invocation processor, creating it lazily.</summary>
    private FunctionInvocationProcessor Processor => field ??= new FunctionInvocationProcessor(
        _logger,
        _activitySource,
        InvokeFunctionAsync);

    /// <summary>
    /// Gets or sets the <see cref="FunctionInvocationContext"/> for the current function invocation.
    /// </summary>
    /// <remarks>
    /// This value flows across async calls.
    /// </remarks>
    public static FunctionInvocationContext? CurrentContext
    {
        get => _currentContext.Value;
        protected set => _currentContext.Value = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether detailed exception information should be included
    /// in the response when calling the underlying <see cref="IRealtimeSession"/>.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the full exception message is added to the response
    /// when calling the underlying <see cref="IRealtimeSession"/>.
    /// <see langword="false"/> if a generic error message is included in the response.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting the value to <see langword="false"/> prevents the underlying model from disclosing
    /// raw exception details to the end user, since it doesn't receive that information. Even in this
    /// case, the raw <see cref="Exception"/> object is available to application code by inspecting
    /// the <see cref="FunctionResultContent.Exception"/> property.
    /// </para>
    /// <para>
    /// Setting the value to <see langword="true"/> can help the underlying <see cref="IRealtimeSession"/> bypass problems on
    /// its own, for example by retrying the function call with different arguments. However it might
    /// result in disclosing the raw exception information to external users, which can be a security
    /// concern depending on the application scenario.
    /// </para>
    /// <para>
    /// Changing the value of this property while the session is in use might result in inconsistencies
    /// as to whether detailed errors are provided during an in-flight request.
    /// </para>
    /// </remarks>
    public bool IncludeDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow concurrent invocation of functions.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if multiple function calls can execute in parallel.
    /// <see langword="false"/> if function calls are processed serially.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// An individual response from the inner session might contain multiple function call requests.
    /// By default, such function calls are processed serially. Set <see cref="AllowConcurrentInvocation"/> to
    /// <see langword="true"/> to enable concurrent invocation such that multiple function calls can execute in parallel.
    /// </remarks>
    public bool AllowConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of iterations per request.
    /// </summary>
    /// <value>
    /// The maximum number of iterations per request.
    /// The default value is 40.
    /// </value>
    /// <remarks>
    /// <para>
    /// Each streaming request to this <see cref="FunctionInvokingRealtimeSession"/> might end up making
    /// multiple function call invocations. Each time the inner session responds with
    /// a function call request, this session might perform that invocation and send the results
    /// back to the inner session. This property limits the number of times
    /// such an invocation is performed.
    /// </para>
    /// <para>
    /// Changing the value of this property while the session is in use might result in inconsistencies
    /// as to how many iterations are allowed for an in-flight request.
    /// </para>
    /// </remarks>
    public int MaximumIterationsPerRequest
    {
        get;
        set
        {
            if (value < 1)
            {
                Throw.ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
        }
    } = 40;

    /// <summary>
    /// Gets or sets the maximum number of consecutive iterations that are allowed to fail with an error.
    /// </summary>
    /// <value>
    /// The maximum number of consecutive iterations that are allowed to fail with an error.
    /// The default value is 3.
    /// </value>
    /// <remarks>
    /// <para>
    /// When function invocations fail with an exception, the <see cref="FunctionInvokingRealtimeSession"/>
    /// continues to send responses to the inner session, optionally supplying exception information (as
    /// controlled by <see cref="IncludeDetailedErrors"/>). This allows the <see cref="IRealtimeSession"/> to
    /// recover from errors by trying other function parameters that might succeed.
    /// </para>
    /// <para>
    /// However, in case function invocations continue to produce exceptions, this property can be used to
    /// limit the number of consecutive failing attempts. When the limit is reached, the exception will be
    /// rethrown to the caller.
    /// </para>
    /// <para>
    /// If the value is set to zero, all function calling exceptions immediately terminate the function
    /// invocation loop and the exception will be rethrown to the caller.
    /// </para>
    /// <para>
    /// Changing the value of this property while the session is in use might result in inconsistencies
    /// as to how many iterations are allowed for an in-flight request.
    /// </para>
    /// </remarks>
    public int MaximumConsecutiveErrorsPerRequest
    {
        get;
        set => field = Throw.IfLessThan(value, 0);
    } = 3;

    /// <summary>Gets or sets a collection of additional tools the session is able to invoke.</summary>
    /// <remarks>
    /// These will not impact the requests sent by the <see cref="FunctionInvokingRealtimeSession"/>, which will pass through the
    /// <see cref="RealtimeClientResponseCreateMessage.Tools" /> unmodified. However, if the inner session requests the invocation of a tool
    /// that was not in <see cref="RealtimeClientResponseCreateMessage.Tools" />, this <see cref="AdditionalTools"/> collection will also be consulted
    /// to look for a corresponding tool to invoke. This is useful when the service might have been preconfigured to be aware
    /// of certain tools that aren't also sent on each individual request.
    /// </remarks>
    public IList<AITool>? AdditionalTools { get; set; }

    /// <summary>Gets or sets a value indicating whether a request to call an unknown function should terminate the function calling loop.</summary>
    /// <value>
    /// <see langword="true"/> to terminate the function calling loop and return the response if a request to call a tool
    /// that isn't available to the <see cref="FunctionInvokingRealtimeSession"/> is received; <see langword="false"/> to create and send a
    /// function result message to the inner session stating that the tool couldn't be found. The default is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When <see langword="false"/>, call requests to any tools that aren't available to the <see cref="FunctionInvokingRealtimeSession"/>
    /// will result in a response message automatically being created and returned to the inner session stating that the tool couldn't be
    /// found. This behavior can help in cases where a model hallucinates a function, but it's problematic if the model has been made aware
    /// of the existence of tools outside of the normal mechanisms, and requests one of those. <see cref="AdditionalTools"/> can be used
    /// to help with that. But if instead the consumer wants to know about all function call requests that the session can't handle,
    /// <see cref="TerminateOnUnknownCalls"/> can be set to <see langword="true"/>. Upon receiving a request to call a function
    /// that the <see cref="FunctionInvokingRealtimeSession"/> doesn't know about, it will terminate the function calling loop and return
    /// the response, leaving the handling of the function call requests to the consumer of the session.
    /// </para>
    /// <para>
    /// <see cref="AITool"/>s that the <see cref="FunctionInvokingRealtimeSession"/> is aware of (for example, because they're in
    /// <see cref="RealtimeClientResponseCreateMessage.Tools"/> or <see cref="AdditionalTools"/>) but that aren't <see cref="AIFunction"/>s aren't considered
    /// unknown, just not invocable. Any requests to a non-invocable tool will also result in the function calling loop terminating,
    /// regardless of <see cref="TerminateOnUnknownCalls"/>.
    /// </para>
    /// </remarks>
    public bool TerminateOnUnknownCalls { get; set; }

    /// <summary>Gets or sets a delegate used to invoke <see cref="AIFunction"/> instances.</summary>
    public Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>? FunctionInvoker { get; set; }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        // Create an activity to group function invocations together for better observability.
        using Activity? activity = FunctionInvocationHelpers.CurrentActivityIsInvokeAgent ? null : _activitySource?.StartActivity(OpenTelemetryConsts.GenAI.OrchestrateToolsName);

        // Track function calls from the client messages
        List<FunctionCallContent>? functionCallContents = null;
        int consecutiveErrorCount = 0;
        int iterationCount = 0;

        await foreach (var message in InnerSession.GetStreamingResponseAsync(updates, cancellationToken).ConfigureAwait(false))
        {
            // Check if this message contains function calls
            bool hasFunctionCalls = false;
            if (message is RealtimeServerResponseOutputItemMessage responseOutputItemMessage && responseOutputItemMessage.Type == RealtimeServerMessageType.ResponseDone)
            {
                // Extract function calls from the message
                functionCallContents ??= [];
                hasFunctionCalls = ExtractFunctionCalls(responseOutputItemMessage, functionCallContents);
            }

            // Always yield the message so consumers can observe function calls and other events.
            yield return message;

            if (hasFunctionCalls)
            {
                if (iterationCount >= MaximumIterationsPerRequest)
                {
                    // Log and stop processing function calls
                    FunctionInvocationLogger.LogMaximumIterationsReached(_logger, MaximumIterationsPerRequest);
                    continue;
                }

                // Check whether the function calls can be handled; if not, terminate the loop.
                if (ShouldTerminateBasedOnFunctionCalls(functionCallContents!))
                {
                    yield break;
                }

                // Process function calls
                iterationCount++;
                var results = await InvokeFunctionsAsync(functionCallContents!, consecutiveErrorCount, cancellationToken).ConfigureAwait(false);

                // Update consecutive error count
                consecutiveErrorCount = results.newConsecutiveErrorCount;

                // Check if we should terminate
                if (results.shouldTerminate)
                {
                    yield break;
                }

                foreach (var resultMessage in results.functionResults)
                {
                    // inject back the function result messages to the inner session
                    await InnerSession.InjectClientMessageAsync(resultMessage, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>Extracts function calls from a realtime server message.</summary>
    private static bool ExtractFunctionCalls(RealtimeServerResponseOutputItemMessage message, List<FunctionCallContent> functionCallContents)
    {
        if (message.Item is null)
        {
            return false;
        }

        functionCallContents.Clear();

        foreach (var content in message.Item.Contents)
        {
            if (content is FunctionCallContent functionCallContent)
            {
                functionCallContents.Add(functionCallContent);
            }
        }

        return functionCallContents.Count > 0;
    }

    /// <summary>Gets whether the function calling loop should exit based on the function call requests.</summary>
    /// <remarks>
    /// This mirrors the logic in <c>FunctionInvokingChatClient.ShouldTerminateLoopBasedOnHandleableFunctions</c>.
    /// If a function call references a non-invocable tool (a declaration but not an <see cref="AIFunction"/>),
    /// the loop always terminates. If the function is completely unknown, the loop terminates only when
    /// <see cref="TerminateOnUnknownCalls"/> is <see langword="true"/>.
    /// </remarks>
    private bool ShouldTerminateBasedOnFunctionCalls(List<FunctionCallContent> functionCallContents)
    {
        var (toolMap, _) = FunctionInvocationHelpers.CreateToolsMap(AdditionalTools, InnerSession.Options?.Tools);

        if (toolMap is null || toolMap.Count == 0)
        {
            // No tools available at all. If TerminateOnUnknownCalls, stop the loop.
            if (TerminateOnUnknownCalls)
            {
                foreach (var fcc in functionCallContents)
                {
                    FunctionInvocationLogger.LogFunctionNotFound(_logger, fcc.Name);
                }

                return true;
            }

            return false;
        }

        foreach (var fcc in functionCallContents)
        {
            if (toolMap.TryGetValue(fcc.Name, out AITool? tool))
            {
                if (tool is not AIFunction)
                {
                    // The tool exists but is not invocable (e.g. AIFunctionDeclaration only).
                    // Always terminate so the caller can handle the call.
                    FunctionInvocationLogger.LogNonInvocableFunction(_logger, fcc.Name);
                    return true;
                }
            }
            else if (TerminateOnUnknownCalls)
            {
                // The tool is completely unknown. If configured, terminate.
                FunctionInvocationLogger.LogFunctionNotFound(_logger, fcc.Name);
                return true;
            }
        }

        return false;
    }

    /// <summary>Invokes the functions and returns results.</summary>
    private async Task<(bool shouldTerminate, int newConsecutiveErrorCount, List<RealtimeClientMessage> functionResults)> InvokeFunctionsAsync(
        List<FunctionCallContent> functionCallContents,
        int consecutiveErrorCount,
        CancellationToken cancellationToken)
    {
        // Compute toolMap to ensure we always use the latest tools
        var (toolMap, _) = FunctionInvocationHelpers.CreateToolsMap(AdditionalTools, InnerSession.Options?.Tools);

        var captureCurrentIterationExceptions = consecutiveErrorCount < MaximumConsecutiveErrorsPerRequest;

        // Use the processor to handle function calls
        var results = await Processor.ProcessFunctionCallsAsync(
            functionCallContents,
            toolMap,
            AllowConcurrentInvocation,
            (callContent, aiFunction, _) => new FunctionInvocationContext
            {
                Function = aiFunction,
                Arguments = new(callContent.Arguments) { Services = FunctionInvocationServices },
                CallContent = callContent
            },
            ctx => CurrentContext = ctx,
            captureCurrentIterationExceptions,
            cancellationToken).ConfigureAwait(false);

        var shouldTerminate = results.Exists(static r => r.Terminate);

        // Update consecutive error count
        bool hasErrors = results.Exists(static r => r.Status == FunctionInvocationStatus.Exception);
        int newConsecutiveErrorCount = hasErrors ? consecutiveErrorCount + 1 : 0;

        // Check if we exceeded the maximum consecutive errors
        if (newConsecutiveErrorCount > MaximumConsecutiveErrorsPerRequest)
        {
            var firstException = results.Find(static r => r.Exception is not null).Exception;
            if (firstException is not null)
            {
                throw firstException;
            }
        }

        // Create function result messages
        var functionResults = CreateFunctionResultMessages(results);

        return (shouldTerminate, newConsecutiveErrorCount, functionResults);
    }

    /// <summary>Creates function result messages from invocation results.</summary>
    private List<RealtimeClientMessage> CreateFunctionResultMessages(List<FunctionInvocationResultInternal> results)
    {
        var messages = new List<RealtimeClientMessage>(results.Count);

        foreach (var result in results)
        {
            // Determine the result value to send back
            object? resultValue = result.Status switch
            {
                FunctionInvocationStatus.RanToCompletion => result.Result,
                FunctionInvocationStatus.NotFound => "Error: Function not found.",
                FunctionInvocationStatus.Exception => IncludeDetailedErrors && result.Exception is not null
                    ? $"Error: {result.Exception.Message}"
                    : "Error: Function invocation failed.",
                _ => "Error: Unknown status."
            };

            // Create the FunctionResultContent
            var functionResultContent = new FunctionResultContent(result.CallContent.CallId, resultValue)
            {
                Exception = result.Exception
            };

            // Create the RealtimeContentItem with the function result
            var contentItem = new RealtimeContentItem([functionResultContent]);

            // Create the conversation item create message
            var message = new RealtimeClientConversationItemCreateMessage(contentItem);
            messages.Add(message);
        }

        // Add a response create message so the model responds to the function results.
        // Do not hardcode output modalities; let the session defaults apply so audio sessions
        // continue to work correctly.
        messages.Add(new RealtimeClientResponseCreateMessage());

        return messages;
    }

    /// <summary>This method will invoke the function within the try block.</summary>
    /// <param name="context">The function invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The function result.</returns>
    protected virtual ValueTask<object?> InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        return FunctionInvoker is { } invoker ?
            invoker(context, cancellationToken) :
            context.Function.InvokeAsync(context.Arguments, cancellationToken);
    }
}

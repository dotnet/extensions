// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.AI.OpenTelemetryConsts.GenAI;

#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that invokes functions defined on <see cref="ChatOptions"/>.
/// Include this in a chat pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// <para>
/// When this client receives a <see cref="FunctionCallContent"/> in a chat response, it responds
/// by calling the corresponding <see cref="AIFunction"/> defined in <see cref="ChatOptions.Tools"/>,
/// producing a <see cref="FunctionResultContent"/> that it sends back to the inner client. This loop
/// is repeated until there are no more function calls to make, or until another stop condition is met,
/// such as hitting <see cref="MaximumIterationsPerRequest"/>.
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="AIFunction"/> instances employed as part of the supplied <see cref="ChatOptions"/> are also safe.
/// The <see cref="AllowConcurrentInvocation"/> property can be used to control whether multiple function invocation
/// requests as part of the same request are invocable concurrently, but even with that set to <see langword="false"/>
/// (the default), multiple concurrent requests to this same instance and using the same tools could result in those
/// tools being used concurrently (one per request). For example, a function that accesses the HttpContext of a specific
/// ASP.NET web request should only be used as part of a single <see cref="ChatOptions"/> at a time, and only with
/// <see cref="AllowConcurrentInvocation"/> set to <see langword="false"/>, in case the inner client decided to issue multiple
/// invocation requests to that same function.
/// </para>
/// </remarks>
public partial class FunctionInvokingChatClient : DelegatingChatClient
{
    /// <summary>The <see cref="FunctionInvocationContext"/> for the current function invocation.</summary>
    private static readonly AsyncLocal<FunctionInvocationContext?> _currentContext = new();

    /// <summary>The logger to use for logging information about function invocation.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="ActivitySource"/> to use for telemetry.</summary>
    /// <remarks>This component does not own the instance and should not dispose it.</remarks>
    private readonly ActivitySource? _activitySource;

    /// <summary>Maximum number of roundtrips allowed to the inner client.</summary>
    private int? _maximumIterationsPerRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>, or the next instance in a chain of clients.</param>
    /// <param name="logger">An <see cref="ILogger"/> to use for logging information about function invocation.</param>
    public FunctionInvokingChatClient(IChatClient innerClient, ILogger? logger = null)
        : base(innerClient)
    {
        _logger = logger ?? NullLogger.Instance;
        _activitySource = innerClient.GetService<ActivitySource>();
    }

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
    /// Gets or sets a value indicating whether to handle exceptions that occur during function calls.
    /// </summary>
    /// <value>
    /// <see langword="false"/> if the
    /// underlying <see cref="IChatClient"/> will be instructed to give a response without invoking
    /// any further functions if a function call fails with an exception.
    /// <see langword="true"/> if the underlying <see cref="IChatClient"/> is allowed
    /// to continue attempting function calls until <see cref="MaximumIterationsPerRequest"/> is reached.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to whether errors are retried during an in-flight request.
    /// </remarks>
    public bool RetryOnError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether detailed exception information should be included
    /// in the chat history when calling the underlying <see cref="IChatClient"/>.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the full exception message is added to the chat history
    /// when calling the underlying <see cref="IChatClient"/>.
    /// <see langword="false"/> if a generic error message is included in the chat history.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting the value to <see langword="false"/> prevents the underlying language model from disclosing
    /// raw exception details to the end user, since it doesn't receive that information. Even in this
    /// case, the raw <see cref="Exception"/> object is available to application code by inspecting
    /// the <see cref="FunctionResultContent.Exception"/> property.
    /// </para>
    /// <para>
    /// Setting the value to <see langword="true"/> can help the underlying <see cref="IChatClient"/> bypass problems on
    /// its own, for example by retrying the function call with different arguments. However it might
    /// result in disclosing the raw exception information to external users, which can be a security
    /// concern depending on the application scenario.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
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
    /// An individual response from the inner client might contain multiple function call requests.
    /// By default, such function calls are processed serially. Set <see cref="AllowConcurrentInvocation"/> to
    /// <see langword="true"/> to enable concurrent invocation such that multiple function calls can execute in parallel.
    /// </remarks>
    public bool AllowConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of iterations per request.
    /// </summary>
    /// <value>
    /// The maximum number of iterations per request.
    /// The default value is <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Each request to this <see cref="FunctionInvokingChatClient"/> might end up making
    /// multiple requests to the inner client. Each time the inner client responds with
    /// a function call request, this client might perform that invocation and send the results
    /// back to the inner client in a new request. This property limits the number of times
    /// such a roundtrip is performed. If null, there is no limit applied. If set, the value
    /// must be at least one, as it includes the initial request.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to how many iterations are allowed for an in-flight request.
    /// </para>
    /// </remarks>
    public int? MaximumIterationsPerRequest
    {
        get => _maximumIterationsPerRequest;
        set
        {
            if (value < 1)
            {
                Throw.ArgumentOutOfRangeException(nameof(value));
            }

            _maximumIterationsPerRequest = value;
        }
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        _ = Throw.IfReadOnly(chatMessages);

        // A single request into this GetResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity(nameof(FunctionInvokingChatClient));

        IList<ChatMessage> originalChatMessages = chatMessages;
        ChatResponse? response = null;
        List<ChatMessage>? responseMessages = null;
        UsageDetails? totalUsage = null;
        List<FunctionCallContent>? functionCallContents = null;

        for (int iteration = 0; ; iteration++)
        {
            functionCallContents?.Clear();

            // Make the call to the handler.
            response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                throw new InvalidOperationException($"The inner {nameof(IChatClient)} returned a null {nameof(ChatResponse)}.");
            }

            // Any function call work to do? If yes, ensure we're tracking that work in functionCallContents.
            bool requiresFunctionInvocation =
                options?.Tools is { Count: > 0 } &&
                (!MaximumIterationsPerRequest.HasValue || iteration < MaximumIterationsPerRequest.GetValueOrDefault()) &&
                CopyFunctionCalls(response.Message.Contents, ref functionCallContents);

            // In the common case where we make a request and there's no function calling work required,
            // fast path out by just returning the original response.
            if (iteration == 0 && !requiresFunctionInvocation)
            {
                return response;
            }

            // Track aggregatable details from the response.
            (responseMessages ??= []).AddRange(response.Messages);
            if (response.Usage is not null)
            {
                if (totalUsage is not null)
                {
                    totalUsage.Add(response.Usage);
                }
                else
                {
                    totalUsage = response.Usage;
                }
            }

            // If there are no tools to call, or for any other reason we should stop, we're done.
            if (!requiresFunctionInvocation)
            {
                // If this is the first request, we can just return the response, as we don't need to
                // incorporate any information from previous requests.
                if (iteration == 0)
                {
                    return response;
                }

                // Otherwise, break out of the loop and allow the handling at the end to configure
                // the response with aggregated data from previous requests.
                break;
            }

            // If the response indicates the inner client is tracking the history, clear it to avoid re-sending the state.
            // In that case, we also avoid touching the user's history, so that we don't need to clear it.
            if (response.ChatThreadId is not null)
            {
                if (chatMessages == originalChatMessages)
                {
                    chatMessages = [];
                }
                else
                {
                    chatMessages.Clear();
                }
            }

            // Add the responses from the function calls into the history.
            var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options!, functionCallContents!, iteration, cancellationToken).ConfigureAwait(false);
            responseMessages.AddRange(modeAndMessages.MessagesAdded);
            if (UpdateOptionsForMode(modeAndMessages.Mode, ref options!, response.ChatThreadId))
            {
                // Terminate
                break;
            }
        }

        Debug.Assert(responseMessages is not null, "Expected to only be here if we have response messages.");
        response.Messages = responseMessages!;
        response.Usage = totalUsage;

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        _ = Throw.IfReadOnly(chatMessages);

        // A single request into this GetStreamingResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity(nameof(FunctionInvokingChatClient));

        List<FunctionCallContent>? functionCallContents = null;
        IList<ChatMessage> originalChatMessages = chatMessages;
        for (int iteration = 0; ; iteration++)
        {
            functionCallContents?.Clear();
            string? chatThreadId = null;

            await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
            {
                if (update is null)
                {
                    throw new InvalidOperationException($"The inner {nameof(IChatClient)} streamed a null {nameof(ChatResponseUpdate)}.");
                }

                chatThreadId ??= update.ChatThreadId;
                _ = CopyFunctionCalls(update.Contents, ref functionCallContents);

                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }

            // If there are no tools to call, or for any other reason we should stop, return the response.
            if (functionCallContents is not { Count: > 0 } ||
                options?.Tools is not { Count: > 0 } ||
                (MaximumIterationsPerRequest is { } maxIterations && iteration >= maxIterations))
            {
                break;
            }

            // If the response indicates the inner client is tracking the history, clear it to avoid re-sending the state.
            // In that case, we also avoid touching the user's history, so that we don't need to clear it.
            if (chatThreadId is not null)
            {
                if (chatMessages == originalChatMessages)
                {
                    chatMessages = [];
                }
                else
                {
                    chatMessages.Clear();
                }
            }

            // Process all of the functions, adding their results into the history.
            var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options, functionCallContents, iteration, cancellationToken).ConfigureAwait(false);
            if (UpdateOptionsForMode(modeAndMessages.Mode, ref options, chatThreadId))
            {
                // Terminate
                yield break;
            }

            // Stream any generated function results. These are already part of the history,
            // but we stream them out for informational purposes.
            string toolResponseId = Guid.NewGuid().ToString("N");
            foreach (var message in modeAndMessages.MessagesAdded)
            {
                var toolResultUpdate = new ChatResponseUpdate
                {
                    AdditionalProperties = message.AdditionalProperties,
                    AuthorName = message.AuthorName,
                    ChatThreadId = chatThreadId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Contents = message.Contents,
                    RawRepresentation = message.RawRepresentation,
                    ResponseId = toolResponseId,
                    Role = message.Role,
                };

                yield return toolResultUpdate;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }
        }
    }

    /// <summary>Copies any <see cref="FunctionCallContent"/> from <paramref name="content"/> to <paramref name="functionCalls"/>.</summary>
    private static bool CopyFunctionCalls(
        IList<AIContent> content, [NotNullWhen(true)] ref List<FunctionCallContent>? functionCalls)
    {
        bool any = false;
        int count = content.Count;
        for (int i = 0; i < count; i++)
        {
            if (content[i] is FunctionCallContent functionCall)
            {
                (functionCalls ??= []).Add(functionCall);
                any = true;
            }
        }

        return any;
    }

    /// <summary>Updates <paramref name="options"/> for the response.</summary>
    /// <returns>true if the function calling loop should terminate; otherwise, false.</returns>
    private static bool UpdateOptionsForMode(ContinueMode mode, ref ChatOptions options, string? chatThreadId)
    {
        switch (mode)
        {
            case ContinueMode.Continue when options.ToolMode is RequiredChatToolMode:
                // We have to reset the tool mode to be non-required after the first iteration,
                // as otherwise we'll be in an infinite loop.
                options = options.Clone();
                options.ToolMode = null;
                if (chatThreadId is not null)
                {
                    options.ChatThreadId = chatThreadId;
                }

                break;

            case ContinueMode.AllowOneMoreRoundtrip:
                // The LLM gets one further chance to answer, but cannot use tools.
                options = options.Clone();
                options.Tools = null;
                options.ToolMode = null;
                if (chatThreadId is not null)
                {
                    options.ChatThreadId = chatThreadId;
                }

                break;

            case ContinueMode.Terminate:
                // Bail immediately.
                return true;

            default:
                // As with the other modes, ensure we've propagated the chat thread ID to the options.
                // We only need to clone the options if we're actually mutating it.
                if (chatThreadId is not null && options.ChatThreadId != chatThreadId)
                {
                    options = options.Clone();
                    options.ChatThreadId = chatThreadId;
                }

                break;
        }

        return false;
    }

    /// <summary>
    /// Processes the function calls in the <paramref name="functionCallContents"/> list.
    /// </summary>
    /// <param name="chatMessages">The current chat contents, inclusive of the function call contents being processed.</param>
    /// <param name="options">The options used for the response being processed.</param>
    /// <param name="functionCallContents">The function call contents representing the functions to be invoked.</param>
    /// <param name="iteration">The iteration number of how many roundtrips have been made to the inner client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ContinueMode"/> value indicating how the caller should proceed.</returns>
    private async Task<(ContinueMode Mode, IList<ChatMessage> MessagesAdded)> ProcessFunctionCallsAsync(
        IList<ChatMessage> chatMessages, ChatOptions options, List<FunctionCallContent> functionCallContents, int iteration, CancellationToken cancellationToken)
    {
        // We must add a response for every tool call, regardless of whether we successfully executed it or not.
        // If we successfully execute it, we'll add the result. If we don't, we'll add an error.

        Debug.Assert(functionCallContents.Count > 0, "Expecteded at least one function call.");

        // Process all functions. If there's more than one and concurrent invocation is enabled, do so in parallel.
        if (functionCallContents.Count == 1)
        {
            FunctionInvocationResult result = await ProcessFunctionCallAsync(
                chatMessages, options, functionCallContents, iteration, 0, cancellationToken).ConfigureAwait(false);
            IList<ChatMessage> added = AddResponseMessages(chatMessages, [result]);
            return (result.ContinueMode, added);
        }
        else
        {
            FunctionInvocationResult[] results;

            if (AllowConcurrentInvocation)
            {
                // Schedule the invocation of every function.
                results = await Task.WhenAll(
                    from i in Enumerable.Range(0, functionCallContents.Count)
                    select Task.Run(() => ProcessFunctionCallAsync(
                        chatMessages, options, functionCallContents,
                        iteration, i, cancellationToken))).ConfigureAwait(false);
            }
            else
            {
                // Invoke each function serially.
                results = new FunctionInvocationResult[functionCallContents.Count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = await ProcessFunctionCallAsync(
                        chatMessages, options, functionCallContents,
                        iteration, i, cancellationToken).ConfigureAwait(false);
                }
            }

            ContinueMode continueMode = ContinueMode.Continue;
            IList<ChatMessage> added = AddResponseMessages(chatMessages, results);
            foreach (FunctionInvocationResult fir in results)
            {
                if (fir.ContinueMode > continueMode)
                {
                    continueMode = fir.ContinueMode;
                }
            }

            return (continueMode, added);
        }
    }

    /// <summary>Processes the function call described in <paramref name="callContents"/>[<paramref name="iteration"/>].</summary>
    /// <param name="chatMessages">The current chat contents, inclusive of the function call contents being processed.</param>
    /// <param name="options">The options used for the response being processed.</param>
    /// <param name="callContents">The function call contents representing all the functions being invoked.</param>
    /// <param name="iteration">The iteration number of how many roundtrips have been made to the inner client.</param>
    /// <param name="functionCallIndex">The 0-based index of the function being called out of <paramref name="callContents"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ContinueMode"/> value indicating how the caller should proceed.</returns>
    private async Task<FunctionInvocationResult> ProcessFunctionCallAsync(
        IList<ChatMessage> chatMessages, ChatOptions options, List<FunctionCallContent> callContents,
        int iteration, int functionCallIndex, CancellationToken cancellationToken)
    {
        var callContent = callContents[functionCallIndex];

        // Look up the AIFunction for the function call. If the requested function isn't available, send back an error.
        AIFunction? function = options.Tools!.OfType<AIFunction>().FirstOrDefault(t => t.Name == callContent.Name);
        if (function is null)
        {
            return new(ContinueMode.Continue, FunctionInvocationStatus.NotFound, callContent, result: null, exception: null);
        }

        FunctionInvocationContext context = new()
        {
            ChatMessages = chatMessages,
            CallContent = callContent,
            Function = function,
            Iteration = iteration,
            FunctionCallIndex = functionCallIndex,
            FunctionCount = callContents.Count,
        };

        object? result;
        try
        {
            result = await InvokeFunctionAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            return new(
                RetryOnError ? ContinueMode.Continue : ContinueMode.AllowOneMoreRoundtrip, // We won't allow further function calls, hence the LLM will just get one more chance to give a final answer.
                FunctionInvocationStatus.Exception,
                callContent,
                result: null,
                exception: e);
        }

        return new(
            context.Terminate ? ContinueMode.Terminate : ContinueMode.Continue,
            FunctionInvocationStatus.RanToCompletion,
            callContent,
            result,
            exception: null);
    }

    /// <summary>Represents the return value of <see cref="ProcessFunctionCallsAsync"/>, dictating how the loop should behave.</summary>
    /// <remarks>These values are ordered from least severe to most severe, and code explicitly depends on the ordering.</remarks>
    internal enum ContinueMode
    {
        /// <summary>Send back the responses and continue processing.</summary>
        Continue = 0,

        /// <summary>Send back the response but without any tools.</summary>
        AllowOneMoreRoundtrip = 1,

        /// <summary>Immediately exit the function calling loop.</summary>
        Terminate = 2,
    }

    /// <summary>Adds one or more response messages for function invocation results.</summary>
    /// <param name="chatMessages">The chat to which to add the one or more response messages.</param>
    /// <param name="results">Information about the function call invocations and results.</param>
    /// <returns>A list of all chat messages added to <paramref name="chatMessages"/>.</returns>
    protected virtual IList<ChatMessage> AddResponseMessages(IList<ChatMessage> chatMessages, ReadOnlySpan<FunctionInvocationResult> results)
    {
        _ = Throw.IfNull(chatMessages);

        var contents = new List<AIContent>(results.Length);
        for (int i = 0; i < results.Length; i++)
        {
            contents.Add(CreateFunctionResultContent(results[i]));
        }

        ChatMessage message = new(ChatRole.Tool, contents);
        chatMessages.Add(message);
        return [message];

        FunctionResultContent CreateFunctionResultContent(FunctionInvocationResult result)
        {
            _ = Throw.IfNull(result);

            object? functionResult;
            if (result.Status == FunctionInvocationStatus.RanToCompletion)
            {
                functionResult = result.Result ?? "Success: Function completed.";
            }
            else
            {
                string message = result.Status switch
                {
                    FunctionInvocationStatus.NotFound => $"Error: Requested function \"{result.CallContent.Name}\" not found.",
                    FunctionInvocationStatus.Exception => "Error: Function failed.",
                    _ => "Error: Unknown error.",
                };

                if (IncludeDetailedErrors && result.Exception is not null)
                {
                    message = $"{message} Exception: {result.Exception.Message}";
                }

                functionResult = message;
            }

            return new FunctionResultContent(result.CallContent.CallId, functionResult) { Exception = result.Exception };
        }
    }

    /// <summary>Invokes the function asynchronously.</summary>
    /// <param name="context">
    /// The function invocation context detailing the function to be invoked and its arguments along with additional request information.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function invocation, or <see langword="null"/> if the function invocation returned <see langword="null"/>.</returns>
    protected virtual async Task<object?> InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        using Activity? activity = _activitySource?.StartActivity(context.Function.Name);

        long startingTimestamp = 0;
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            startingTimestamp = Stopwatch.GetTimestamp();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokingSensitive(context.Function.Name, LoggingHelpers.AsJson(context.CallContent.Arguments, context.Function.JsonSerializerOptions));
            }
            else
            {
                LogInvoking(context.Function.Name);
            }
        }

        object? result = null;
        try
        {
            CurrentContext = context;
            result = await context.Function.InvokeAsync(context.CallContent.Arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (activity is not null)
            {
                _ = activity.SetTag("error.type", e.GetType().FullName)
                            .SetStatus(ActivityStatusCode.Error, e.Message);
            }

            if (e is OperationCanceledException)
            {
                LogInvocationCanceled(context.Function.Name);
            }
            else
            {
                LogInvocationFailed(context.Function.Name, e);
            }

            throw;
        }
        finally
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                TimeSpan elapsed = GetElapsedTime(startingTimestamp);

                if (result is not null && _logger.IsEnabled(LogLevel.Trace))
                {
                    LogInvocationCompletedSensitive(context.Function.Name, elapsed, LoggingHelpers.AsJson(result, context.Function.JsonSerializerOptions));
                }
                else
                {
                    LogInvocationCompleted(context.Function.Name, elapsed);
                }
            }
        }

        return result;
    }

    private static TimeSpan GetElapsedTime(long startingTimestamp) =>
#if NET
        Stopwatch.GetElapsedTime(startingTimestamp);
#else
        new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
#endif

    [LoggerMessage(LogLevel.Debug, "Invoking {MethodName}.", SkipEnabledCheck = true)]
    private partial void LogInvoking(string methodName);

    [LoggerMessage(LogLevel.Trace, "Invoking {MethodName}({Arguments}).", SkipEnabledCheck = true)]
    private partial void LogInvokingSensitive(string methodName, string arguments);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation completed. Duration: {Duration}", SkipEnabledCheck = true)]
    private partial void LogInvocationCompleted(string methodName, TimeSpan duration);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invocation completed. Duration: {Duration}. Result: {Result}", SkipEnabledCheck = true)]
    private partial void LogInvocationCompletedSensitive(string methodName, TimeSpan duration, string result);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} invocation failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);

    /// <summary>Provides information about the invocation of a function call.</summary>
    public sealed class FunctionInvocationResult
    {
        internal FunctionInvocationResult(ContinueMode continueMode, FunctionInvocationStatus status, FunctionCallContent callContent, object? result, Exception? exception)
        {
            ContinueMode = continueMode;
            Status = status;
            CallContent = callContent;
            Result = result;
            Exception = exception;
        }

        /// <summary>Gets status about how the function invocation completed.</summary>
        public FunctionInvocationStatus Status { get; }

        /// <summary>Gets the function call content information associated with this invocation.</summary>
        public FunctionCallContent CallContent { get; }

        /// <summary>Gets the result of the function call.</summary>
        public object? Result { get; }

        /// <summary>Gets any exception the function call threw.</summary>
        public Exception? Exception { get; }

        /// <summary>Gets an indication for how the caller should continue the processing loop.</summary>
        internal ContinueMode ContinueMode { get; }
    }

    /// <summary>Provides error codes for when errors occur as part of the function calling loop.</summary>
    public enum FunctionInvocationStatus
    {
        /// <summary>The operation completed successfully.</summary>
        RanToCompletion,

        /// <summary>The requested function could not be found.</summary>
        NotFound,

        /// <summary>The function call failed with an exception.</summary>
        Exception,
    }
}

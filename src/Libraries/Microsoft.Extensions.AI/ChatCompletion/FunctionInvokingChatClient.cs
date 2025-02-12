// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA2213 // Disposable fields should be disposed

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that invokes functions defined on <see cref="ChatOptions"/>.
/// Include this in a chat pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// <para>
/// When this client receives a <see cref="FunctionCallContent"/> in a chat response, it responds
/// by calling the corresponding <see cref="AIFunction"/> defined in <see cref="ChatOptions"/>,
/// producing a <see cref="FunctionResultContent"/>.
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="AIFunction"/> instances employed as part of the supplied <see cref="ChatOptions"/> are also safe.
/// The <see cref="ConcurrentInvocation"/> property can be used to control whether multiple function invocation
/// requests as part of the same request are invocable concurrently, but even with that set to <see langword="false"/>
/// (the default), multiple concurrent requests to this same instance and using the same tools could result in those
/// tools being used concurrently (one per request). For example, a function that accesses the HttpContext of a specific
/// ASP.NET web request should only be used as part of a single <see cref="ChatOptions"/> at a time, and only with
/// <see cref="ConcurrentInvocation"/> set to <see langword="false"/>, in case the inner client decided to issue multiple
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
    /// Gets or sets the <see cref="FunctionInvocationContext"/> for the current function invocation.
    /// </summary>
    /// <remarks>
    /// This value flows across async calls.
    /// </remarks>
    public static FunctionInvocationContext? CurrentContext
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }

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
    public bool DetailedErrors { get; set; }

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
    /// By default, such function calls are processed serially. Set <see cref="ConcurrentInvocation"/> to
    /// <see langword="true"/> to enable concurrent invocation such that multiple function calls can execute in parallel.
    /// </remarks>
    public bool ConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to keep intermediate function calling request
    /// and response messages in the chat history.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if intermediate messages persist in the <see cref="IList{ChatMessage}"/> list provided
    /// to <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/> by the caller.
    /// <see langword="false"/> if intermediate messages are removed prior to completing the operation.
    /// The default value is <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When the inner <see cref="IChatClient"/> returns <see cref="FunctionCallContent"/> to the
    /// <see cref="FunctionInvokingChatClient"/>, the <see cref="FunctionInvokingChatClient"/> adds
    /// those messages to the list of messages, along with <see cref="FunctionResultContent"/> instances
    /// it creates with the results of invoking the requested functions. The resulting augmented
    /// list of messages is then passed to the inner client in order to send the results back.
    /// By default, those messages persist in the <see cref="IList{ChatMessage}"/> list provided to
    /// <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/> by the caller, such that those
    /// messages are available to the caller. Set <see cref="KeepFunctionCallingMessages"/> to avoid including
    /// those messages in the caller-provided <see cref="IList{ChatMessage}"/>.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to whether function calling messages are kept during an in-flight request.
    /// </para>
    /// <para>
    /// If the underlying <see cref="IChatClient"/> responds with <see cref="ChatResponse.ChatThreadId"/>
    /// set to a non-<see langword="null"/> value, this property may be ignored and behave as if it is
    /// <see langword="false"/>, with any such intermediate messages not stored in the messages list.
    /// </para>
    /// </remarks>
    public bool KeepFunctionCallingMessages { get; set; } = true;

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

        // A single request into this GetResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity(nameof(FunctionInvokingChatClient));

        ChatResponse? response = null;
        UsageDetails? totalUsage = null;
        IList<ChatMessage> originalChatMessages = chatMessages;
        try
        {
            for (int iteration = 0; ; iteration++)
            {
                // Make the call to the handler.
                response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

                // Aggregate usage data over all calls
                if (response.Usage is not null)
                {
                    totalUsage ??= new();
                    totalUsage.Add(response.Usage);
                }

                // If there are no tools to call, or for any other reason we should stop, return the response.
                if (options is null
                    || options.Tools is not { Count: > 0 }
                    || response.Choices.Count == 0
                    || (MaximumIterationsPerRequest is { } maxIterations && iteration >= maxIterations))
                {
                    break;
                }

                // If there's more than one choice, we don't know which one to add to chat history, or which
                // of their function calls to process. This should not happen except if the developer has
                // explicitly requested multiple choices. We fail aggressively to avoid cases where a developer
                // doesn't realize this and is wasting their budget requesting extra choices we'd never use.
                if (response.Choices.Count > 1)
                {
                    ThrowForMultipleChoices();
                }

                // Extract any function call contents on the first choice. If there are none, we're done.
                // We don't have any way to express a preference to use a different choice, since this
                // is a niche case especially with function calling.
                FunctionCallContent[] functionCallContents = response.Message.Contents.OfType<FunctionCallContent>().ToArray();
                if (functionCallContents.Length == 0)
                {
                    break;
                }

                // Update the chat history. If the underlying client is tracking the state, then we want to avoid re-sending
                // what we already sent as well as this response message, so create a new list to store the response message(s).
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
                else
                {
                    // Otherwise, we need to add the response message to the history we're sending back. However, if the caller
                    // doesn't want the intermediate messages, create a new list that we mutate instead of mutating the original.
                    if (!KeepFunctionCallingMessages)
                    {
                        // Create a new list that will include the message with the function call contents.
                        if (chatMessages == originalChatMessages)
                        {
                            chatMessages = [.. chatMessages];
                        }

                        // We want to include any non-functional calling content, if there is any,
                        // in the caller's list so that they don't lose out on actual content.
                        // This can happen but is relatively rare.
                        if (response.Message.Contents.Any(c => c is not FunctionCallContent))
                        {
                            var clone = response.Message.Clone();
                            clone.Contents = clone.Contents.Where(c => c is not FunctionCallContent).ToList();
                            originalChatMessages.Add(clone);
                        }
                    }

                    // Add the original response message into the history.
                    chatMessages.Add(response.Message);
                }

                // Add the responses from the function calls into the history.
                var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options, functionCallContents, iteration, cancellationToken).ConfigureAwait(false);
                if (UpdateOptionsForMode(modeAndMessages.Mode, ref options, response.ChatThreadId))
                {
                    // Terminate
                    return response;
                }
            }

            return response;
        }
        finally
        {
            if (response is not null)
            {
                response.Usage = totalUsage;
            }
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        // A single request into this GetStreamingResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity(nameof(FunctionInvokingChatClient));

        List<FunctionCallContent> functionCallContents = [];
        int? choice;
        IList<ChatMessage> originalChatMessages = chatMessages;
        for (int iteration = 0; ; iteration++)
        {
            choice = null;
            string? chatThreadId = null;
            functionCallContents.Clear();
            await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
            {
                // We're going to emit all StreamingChatMessage items upstream, even ones that represent
                // function calls, because a given StreamingChatMessage can contain other content, too.
                // And if we yield the function calls, and the consumer adds all the content into a message
                // that's then added into history, they'll end up with function call contents that aren't
                // directly paired with function result contents, which may cause issues for some models
                // when the history is later sent again.

                // Find all the FCCs. We need to track these separately in order to be able to process them later.
                int preFccCount = functionCallContents.Count;
                functionCallContents.AddRange(update.Contents.OfType<FunctionCallContent>());

                // If there were any, remove them from the update. We do this before yielding the update so
                // that we're not modifying an instance already provided back to the caller.
                int addedFccs = functionCallContents.Count - preFccCount;
                if (addedFccs > 0)
                {
                    update.Contents = addedFccs == update.Contents.Count ?
                        [] : update.Contents.Where(c => c is not FunctionCallContent).ToList();
                }

                // Only one choice is allowed with automatic function calling.
                if (choice is null)
                {
                    choice = update.ChoiceIndex;
                }
                else if (choice != update.ChoiceIndex)
                {
                    ThrowForMultipleChoices();
                }

                chatThreadId ??= update.ChatThreadId;

                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }

            // If there are no tools to call, or for any other reason we should stop, return the response.
            if (options is null
                || options.Tools is not { Count: > 0 }
                || (MaximumIterationsPerRequest is { } maxIterations && iteration >= maxIterations)
                || functionCallContents is not { Count: > 0 })
            {
                break;
            }

            // Update the chat history. If the underlying client is tracking the state, then we want to avoid re-sending
            // what we already sent as well as this response message, so create a new list to store the response message(s).
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
            else
            {
                // Otherwise, we need to add the response message to the history we're sending back. However, if the caller
                // doesn't want the intermediate messages, create a new list that we mutate instead of mutating the original.
                if (chatMessages == originalChatMessages && !KeepFunctionCallingMessages)
                {
                    chatMessages = [.. chatMessages];
                }

                // Add a manufactured response message containing the function call contents to the chat history.
                chatMessages.Add(new(ChatRole.Assistant, [.. functionCallContents]));
            }

            // Process all of the functions, adding their results into the history.
            var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options, functionCallContents, iteration, cancellationToken).ConfigureAwait(false);
            if (UpdateOptionsForMode(modeAndMessages.Mode, ref options, chatThreadId))
            {
                // Terminate
                yield break;
            }
        }
    }

    /// <summary>Throws an exception when multiple choices are received.</summary>
    private static void ThrowForMultipleChoices()
    {
        // If there's more than one choice, we don't know which one to add to chat history, or which
        // of their function calls to process. This should not happen except if the developer has
        // explicitly requested multiple choices. We fail aggressively to avoid cases where a developer
        // doesn't realize this and is wasting their budget requesting extra choices we'd never use.
        throw new InvalidOperationException("Automatic function call invocation only accepts a single choice, but multiple choices were received.");
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
        IList<ChatMessage> chatMessages, ChatOptions options, IReadOnlyList<FunctionCallContent> functionCallContents, int iteration, CancellationToken cancellationToken)
    {
        // We must add a response for every tool call, regardless of whether we successfully executed it or not.
        // If we successfully execute it, we'll add the result. If we don't, we'll add an error.

        int functionCount = functionCallContents.Count;
        Debug.Assert(functionCount > 0, $"Expecteded {nameof(functionCount)} to be > 0, got {functionCount}.");

        // Process all functions. If there's more than one and concurrent invocation is enabled, do so in parallel.
        if (functionCount == 1)
        {
            FunctionInvocationResult result = await ProcessFunctionCallAsync(chatMessages, options, functionCallContents[0], iteration, 0, 1, cancellationToken).ConfigureAwait(false);
            IList<ChatMessage> added = AddResponseMessages(chatMessages, [result]);
            return (result.ContinueMode, added);
        }
        else
        {
            FunctionInvocationResult[] results;

            if (ConcurrentInvocation)
            {
                // Schedule the invocation of every function.
                results = await Task.WhenAll(
                    from i in Enumerable.Range(0, functionCount)
                    select Task.Run(() => ProcessFunctionCallAsync(chatMessages, options, functionCallContents[i], iteration, i, functionCount, cancellationToken))).ConfigureAwait(false);
            }
            else
            {
                // Invoke each function serially.
                results = new FunctionInvocationResult[functionCount];
                for (int i = 0; i < functionCount; i++)
                {
                    results[i] = await ProcessFunctionCallAsync(chatMessages, options, functionCallContents[i], iteration, i, functionCount, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Processes the function call described in <paramref name="functionCallContent"/>.</summary>
    /// <param name="chatMessages">The current chat contents, inclusive of the function call contents being processed.</param>
    /// <param name="options">The options used for the response being processed.</param>
    /// <param name="functionCallContent">The function call content representing the function to be invoked.</param>
    /// <param name="iteration">The iteration number of how many roundtrips have been made to the inner client.</param>
    /// <param name="functionCallIndex">The 0-based index of the function being called out of <paramref name="totalFunctionCount"/> total functions.</param>
    /// <param name="totalFunctionCount">The number of function call requests made, of which this is one.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ContinueMode"/> value indicating how the caller should proceed.</returns>
    private async Task<FunctionInvocationResult> ProcessFunctionCallAsync(
        IList<ChatMessage> chatMessages, ChatOptions options, FunctionCallContent functionCallContent,
        int iteration, int functionCallIndex, int totalFunctionCount, CancellationToken cancellationToken)
    {
        // Look up the AIFunction for the function call. If the requested function isn't available, send back an error.
        AIFunction? function = options.Tools!.OfType<AIFunction>().FirstOrDefault(t => t.Metadata.Name == functionCallContent.Name);
        if (function is null)
        {
            return new(ContinueMode.Continue, FunctionStatus.NotFound, functionCallContent, result: null, exception: null);
        }

        FunctionInvocationContext context = new(chatMessages, functionCallContent, function)
        {
            Iteration = iteration,
            FunctionCallIndex = functionCallIndex,
            FunctionCount = totalFunctionCount,
        };

        try
        {
            object? result = await InvokeFunctionAsync(context, cancellationToken).ConfigureAwait(false);
            return new(
                context.Terminate ? ContinueMode.Terminate : ContinueMode.Continue,
                FunctionStatus.CompletedSuccessfully,
                functionCallContent,
                result,
                exception: null);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            return new(
                RetryOnError ? ContinueMode.Continue : ContinueMode.AllowOneMoreRoundtrip, // We won't allow further function calls, hence the LLM will just get one more chance to give a final answer.
                FunctionStatus.Failed,
                functionCallContent,
                result: null,
                exception: e);
        }
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
    /// <param name="chat">The chat to which to add the one or more response messages.</param>
    /// <param name="results">Information about the function call invocations and results.</param>
    /// <returns>A list of all chat messages added to <paramref name="chat"/>.</returns>
    protected virtual IList<ChatMessage> AddResponseMessages(IList<ChatMessage> chat, ReadOnlySpan<FunctionInvocationResult> results)
    {
        _ = Throw.IfNull(chat);

        var contents = new AIContent[results.Length];
        for (int i = 0; i < results.Length; i++)
        {
            contents[i] = CreateFunctionResultContent(results[i]);
        }

        ChatMessage message = new(ChatRole.Tool, contents);
        chat.Add(message);
        return [message];

        FunctionResultContent CreateFunctionResultContent(FunctionInvocationResult result)
        {
            _ = Throw.IfNull(result);

            object? functionResult;
            if (result.Status == FunctionStatus.CompletedSuccessfully)
            {
                functionResult = result.Result ?? "Success: Function completed.";
            }
            else
            {
                string message = result.Status switch
                {
                    FunctionStatus.NotFound => $"Error: Requested function \"{result.CallContent.Name}\" not found.",
                    FunctionStatus.Failed => "Error: Function failed.",
                    _ => "Error: Unknown error.",
                };

                if (DetailedErrors && result.Exception is not null)
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

        using Activity? activity = _activitySource?.StartActivity(context.Function.Metadata.Name);

        long startingTimestamp = 0;
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            startingTimestamp = Stopwatch.GetTimestamp();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokingSensitive(context.Function.Metadata.Name, LoggingHelpers.AsJson(context.CallContent.Arguments, context.Function.Metadata.JsonSerializerOptions));
            }
            else
            {
                LogInvoking(context.Function.Metadata.Name);
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
                LogInvocationCanceled(context.Function.Metadata.Name);
            }
            else
            {
                LogInvocationFailed(context.Function.Metadata.Name, e);
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
                    LogInvocationCompletedSensitive(context.Function.Metadata.Name, elapsed, LoggingHelpers.AsJson(result, context.Function.Metadata.JsonSerializerOptions));
                }
                else
                {
                    LogInvocationCompleted(context.Function.Metadata.Name, elapsed);
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

    /// <summary>Provides context for a function invocation.</summary>
    public sealed class FunctionInvocationContext
    {
        /// <summary>Initializes a new instance of the <see cref="FunctionInvocationContext"/> class.</summary>
        /// <param name="chatMessages">The chat contents associated with the operation that initiated this function call request.</param>
        /// <param name="functionCallContent">The AI function to be invoked.</param>
        /// <param name="function">The function call content information associated with this invocation.</param>
        internal FunctionInvocationContext(
            IList<ChatMessage> chatMessages,
            FunctionCallContent functionCallContent,
            AIFunction function)
        {
            Function = function;
            CallContent = functionCallContent;
            ChatMessages = chatMessages;
        }

        /// <summary>Gets or sets the AI function to be invoked.</summary>
        public AIFunction Function { get; set; }

        /// <summary>Gets or sets the function call content information associated with this invocation.</summary>
        public FunctionCallContent CallContent { get; set; }

        /// <summary>Gets or sets the chat contents associated with the operation that initiated this function call request.</summary>
        public IList<ChatMessage> ChatMessages { get; set; }

        /// <summary>Gets or sets the number of this iteration with the underlying client.</summary>
        /// <remarks>
        /// The initial request to the client that passes along the chat contents provided to the <see cref="FunctionInvokingChatClient"/>
        /// is iteration 1. If the client responds with a function call request, the next request to the client is iteration 2, and so on.
        /// </remarks>
        public int Iteration { get; set; }

        /// <summary>Gets or sets the index of the function call within the iteration.</summary>
        /// <remarks>
        /// The response from the underlying client may include multiple function call requests.
        /// This index indicates the position of the function call within the iteration.
        /// </remarks>
        public int FunctionCallIndex { get; set; }

        /// <summary>Gets or sets the total number of function call requests within the iteration.</summary>
        /// <remarks>
        /// The response from the underlying client might include multiple function call requests.
        /// This count indicates how many there were.
        /// </remarks>
        public int FunctionCount { get; set; }

        /// <summary>Gets or sets a value indicating whether to terminate the request.</summary>
        /// <remarks>
        /// In response to a function call request, the function might be invoked, its result added to the chat contents,
        /// and a new request issued to the wrapped client. If this property is set to <see langword="true"/>, that subsequent request
        /// will not be issued and instead the loop immediately terminated rather than continuing until there are no
        /// more function call requests in responses.
        /// </remarks>
        public bool Terminate { get; set; }
    }

    /// <summary>Provides information about the invocation of a function call.</summary>
    public sealed class FunctionInvocationResult
    {
        internal FunctionInvocationResult(ContinueMode continueMode, FunctionStatus status, FunctionCallContent callContent, object? result, Exception? exception)
        {
            ContinueMode = continueMode;
            Status = status;
            CallContent = callContent;
            Result = result;
            Exception = exception;
        }

        /// <summary>Gets status about how the function invocation completed.</summary>
        public FunctionStatus Status { get; }

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
    public enum FunctionStatus
    {
        /// <summary>The operation completed successfully.</summary>
        CompletedSuccessfully,

        /// <summary>The requested function could not be found.</summary>
        NotFound,

        /// <summary>The function call failed with an exception.</summary>
        Failed,
    }
}

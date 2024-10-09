// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that invokes functions defined on <see cref="ChatOptions"/>.
/// Include this in a chat pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// When this client receives a <see cref="FunctionCallContent"/> in a chat completion, it responds
/// by calling the corresponding <see cref="AIFunction"/> defined in <see cref="ChatOptions"/>,
/// producing a <see cref="FunctionResultContent"/>.
/// </remarks>
public class FunctionInvokingChatClient : DelegatingChatClient
{
    /// <summary>Maximum number of roundtrips allowed to the inner client.</summary>
    private int? _maximumIterationsPerRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>, or the next instance in a chain of clients.</param>
    public FunctionInvokingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to handle exceptions that occur during function calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the value is <see langword="false"/>, then if a function call fails with an exception, the
    /// underlying <see cref="IChatClient"/> will be instructed to give a response without invoking
    /// any further functions.
    /// </para>
    /// <para>
    /// If the value is <see langword="true"/>, the underlying <see cref="IChatClient"/> will be allowed
    /// to continue attempting function calls until <see cref="MaximumIterationsPerRequest"/> is reached.
    /// </para>
    /// <para>
    /// The default value is <see langword="false"/>.
    /// </para>
    /// </remarks>
    public bool RetryOnError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether detailed exception information should be included
    /// in the chat history when calling the underlying <see cref="IChatClient"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see langword="false"/>, meaning that only a generic error message will
    /// be included in the chat history. This prevents the underlying language model from disclosing
    /// raw exception details to the end user, since it does not receive that information. Even in this
    /// case, the raw <see cref="Exception"/> object is available to application code by inspecting
    /// the <see cref="FunctionResultContent.Exception"/> property.
    /// </para>
    /// <para>
    /// If set to <see langword="true"/>, the full exception message will be added to the chat history
    /// when calling the underlying <see cref="IChatClient"/>. This can help it to bypass problems on
    /// its own, for example by retrying the function call with different arguments. However it may
    /// result in disclosing the raw exception information to external users, which may be a security
    /// concern depending on the application scenario.
    /// </para>
    /// </remarks>
    public bool DetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow concurrent invocation of functions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An individual response from the inner client may contain multiple function call requests.
    /// By default, such function calls are processed serially. Set <see cref="ConcurrentInvocation"/> to
    /// <see langword="true"/> to enable concurrent invocation such that multiple function calls may execute in parallel.
    /// </para>
    /// <para>
    /// The default value is <see langword="false"/>.
    /// </para>
    /// </remarks>
    public bool ConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to keep intermediate messages in the chat history.
    /// </summary>
    /// <remarks>
    /// When the inner <see cref="IChatClient"/> returns <see cref="FunctionCallContent"/> to the
    /// <see cref="FunctionInvokingChatClient"/>, the <see cref="FunctionInvokingChatClient"/> adds
    /// those messages to the list of messages, along with <see cref="FunctionResultContent"/> instances
    /// it creates with the results of invoking the requested functions. The resulting augmented
    /// list of messages is then passed to the inner client in order to send the results back.
    /// By default, <see cref="KeepFunctionCallingMessages"/> is <see langword="true"/>, and those
    /// messages will persist in the <see cref="IList{ChatMessage}"/> list provided to <see cref="CompleteAsync"/>
    /// and <see cref="CompleteStreamingAsync"/> by the caller. Set <see cref="KeepFunctionCallingMessages"/>
    /// to <see langword="false"/> to remove those messages prior to completing the operation.
    /// </remarks>
    public bool KeepFunctionCallingMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of iterations per request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each request to this <see cref="FunctionInvokingChatClient"/> may end up making
    /// multiple requests to the inner client. Each time the inner client responds with
    /// a function call request, this client may perform that invocation and send the results
    /// back to the inner client in a new request. This property limits the number of times
    /// such a roundtrip is performed. If null, there is no limit applied. If set, the value
    /// must be at least one, as it includes the initial request.
    /// </para>
    /// <para>
    /// The default value is <see langword="null"/>.
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
    public override async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        ChatCompletion? response;

        HashSet<ChatMessage>? messagesToRemove = null;
        HashSet<AIContent>? contentsToRemove = null;
        try
        {
            for (int iteration = 0; ; iteration++)
            {
                // Make the call to the handler.
                response = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

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
                    throw new InvalidOperationException($"Automatic function call invocation only accepts a single choice, but {response.Choices.Count} choices were received.");
                }

                // Extract any function call contents on the first choice. If there are none, we're done.
                // We don't have any way to express a preference to use a different choice, since this
                // is a niche case especially with function calling.
                FunctionCallContent[] functionCallContents = response.Message.Contents.OfType<FunctionCallContent>().ToArray();
                if (functionCallContents.Length == 0)
                {
                    break;
                }

                // Track all added messages in order to remove them, if requested.
                if (!KeepFunctionCallingMessages)
                {
                    messagesToRemove ??= [];
                }

                // Add the original response message into the history and track the message for removal.
                chatMessages.Add(response.Message);
                if (messagesToRemove is not null)
                {
                    if (functionCallContents.Length == response.Message.Contents.Count)
                    {
                        // The most common case is that the response message contains only function calling content.
                        // In that case, we can just track the whole message for removal.
                        _ = messagesToRemove.Add(response.Message);
                    }
                    else
                    {
                        // In the less likely case where some content is function calling and some isn't, we don't want to remove
                        // the non-function calling content by removing the whole message. So we track the content directly.
                        (contentsToRemove ??= []).UnionWith(functionCallContents);
                    }
                }

                // Add the responses from the function calls into the history.
                var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options, functionCallContents, iteration, cancellationToken).ConfigureAwait(false);
                if (modeAndMessages.MessagesAdded is not null)
                {
                    messagesToRemove?.UnionWith(modeAndMessages.MessagesAdded);
                }

                switch (modeAndMessages.Mode)
                {
                    case ContinueMode.Continue when options.ToolMode is RequiredChatToolMode:
                        // We have to reset this after the first iteration, otherwise we'll be in an infinite loop.
                        options = options.Clone();
                        options.ToolMode = ChatToolMode.Auto;
                        break;

                    case ContinueMode.AllowOneMoreRoundtrip:
                        // The LLM gets one further chance to answer, but cannot use tools.
                        options = options.Clone();
                        options.Tools = null;
                        break;

                    case ContinueMode.Terminate:
                        // Bail immediately.
                        return response;
                }
            }

            return response!;
        }
        finally
        {
            RemoveMessagesAndContentFromList(messagesToRemove, contentsToRemove, chatMessages);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        HashSet<ChatMessage>? messagesToRemove = null;
        try
        {
            for (int iteration = 0; ; iteration++)
            {
                List<FunctionCallContent>? functionCallContents = null;
                await foreach (var chunk in base.CompleteStreamingAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
                {
                    // We're going to emit all StreamingChatMessage items upstream, even ones that represent
                    // function calls, because a given StreamingChatMessage can contain other content too.
                    yield return chunk;

                    foreach (var item in chunk.Contents.OfType<FunctionCallContent>())
                    {
                        functionCallContents ??= [];
                        functionCallContents.Add(item);
                    }
                }

                // If there are no tools to call, or for any other reason we should stop, return the response.
                if (options is null
                    || options.Tools is not { Count: > 0 }
                    || (MaximumIterationsPerRequest is { } maxIterations && iteration >= maxIterations)
                    || functionCallContents is not { Count: > 0 })
                {
                    break;
                }

                // Track all added messages in order to remove them, if requested.
                if (!KeepFunctionCallingMessages)
                {
                    messagesToRemove ??= [];
                }

                // Add a manufactured response message containing the function call contents to the chat history.
                ChatMessage functionCallMessage = new(ChatRole.Assistant, [.. functionCallContents]);
                chatMessages.Add(functionCallMessage);
                _ = messagesToRemove?.Add(functionCallMessage);

                // Process all of the functions, adding their results into the history.
                var modeAndMessages = await ProcessFunctionCallsAsync(chatMessages, options, functionCallContents, iteration, cancellationToken).ConfigureAwait(false);
                if (modeAndMessages.MessagesAdded is not null)
                {
                    messagesToRemove?.UnionWith(modeAndMessages.MessagesAdded);
                }

                // Decide how to proceed based on the result of the function calls.
                switch (modeAndMessages.Mode)
                {
                    case ContinueMode.Continue when options.ToolMode is RequiredChatToolMode:
                        // We have to reset this after the first iteration, otherwise we'll be in an infinite loop.
                        options = options.Clone();
                        options.ToolMode = ChatToolMode.Auto;
                        break;

                    case ContinueMode.AllowOneMoreRoundtrip:
                        // The LLM gets one further chance to answer, but cannot use tools.
                        options = options.Clone();
                        options.Tools = null;
                        break;

                    case ContinueMode.Terminate:
                        // Bail immediately.
                        yield break;
                }
            }
        }
        finally
        {
            RemoveMessagesAndContentFromList(messagesToRemove, contentToRemove: null, chatMessages);
        }
    }

    /// <summary>
    /// Removes all of the messages in <paramref name="messagesToRemove"/> from <paramref name="messages"/>
    /// and all of the content in <paramref name="contentToRemove"/> from the messages in <paramref name="messages"/>.
    /// </summary>
    private static void RemoveMessagesAndContentFromList(
        HashSet<ChatMessage>? messagesToRemove,
        HashSet<AIContent>? contentToRemove,
        IList<ChatMessage> messages)
    {
        Debug.Assert(
            contentToRemove is null || messagesToRemove is not null,
            "We should only be tracking content to remove if we're also tracking messages to remove.");

        if (messagesToRemove is not null)
        {
            for (int m = messages.Count - 1; m >= 0; m--)
            {
                ChatMessage message = messages[m];

                if (contentToRemove is not null)
                {
                    for (int c = message.Contents.Count - 1; c >= 0; c--)
                    {
                        if (contentToRemove.Contains(message.Contents[c]))
                        {
                            message.Contents.RemoveAt(c);
                        }
                    }
                }

                if (messages.Count == 0 || messagesToRemove.Contains(messages[m]))
                {
                    messages.RemoveAt(m);
                }
            }
        }
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
                    FunctionStatus.NotFound => "Error: Requested function not found.",
                    FunctionStatus.Failed => "Error: Function failed.",
                    _ => "Error: Unknown error.",
                };

                if (DetailedErrors && result.Exception is not null)
                {
                    message = $"{message} Exception: {result.Exception.Message}";
                }

                functionResult = message;
            }

            return new FunctionResultContent(result.CallContent.CallId, result.CallContent.Name, functionResult, result.Exception);
        }
    }

    /// <summary>Invokes the function asynchronously.</summary>
    /// <param name="context">
    /// The function invocation context detailing the function to be invoked and its arguments along with additional request information.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function invocation. This may be null if the function invocation returned null.</returns>
    protected virtual Task<object?> InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        return context.Function.InvokeAsync(context.CallContent.Arguments, cancellationToken);
    }

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
        /// The response from the underlying client may include multiple function call requests.
        /// This count indicates how many there were.
        /// </remarks>
        public int FunctionCount { get; set; }

        /// <summary>Gets or sets a value indicating whether to terminate the request.</summary>
        /// <remarks>
        /// In response to a function call request, the function may be invoked, its result added to the chat contents,
        /// and a new request issued to the wrapped client. If this property is set to true, that subsequent request
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

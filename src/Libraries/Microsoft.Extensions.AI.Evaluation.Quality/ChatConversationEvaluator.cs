// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement an AI-based <see cref="IEvaluator"/>.
/// </summary>
public abstract class ChatConversationEvaluator : IEvaluator
{
    /// <inheritdoc/>
    public abstract IReadOnlyCollection<string> EvaluationMetricNames { get; }

    /// <summary>
    /// Gets the <see cref="AI.ChatOptions"/> that this <see cref="IEvaluator"/> uses when performing evaluations.
    /// </summary>
    protected virtual ChatOptions? ChatOptions => null;

    /// <summary>
    /// Gets a value indicating whether this <see cref="IEvaluator"/> considers the entire conversation history (in
    /// addition to the request and response being evaluated) as part of the evaluation it performs.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this <see cref="IEvaluator"/> considers the entire conversation history as part of
    /// the evaluation it performs; <see langword="false"/> otherwise.
    /// </value>
    protected abstract bool IgnoresHistory { get; }

    /// <summary>
    /// Gets the system prompt that this <see cref="IEvaluator"/> uses when performing evaluations.
    /// </summary>
    protected virtual string? SystemPrompt => null;

    /// <inheritdoc/>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse, nameof(modelResponse));
        _ = Throw.IfNull(chatConfiguration, nameof(chatConfiguration));

        EvaluationResult result = InitializeResult();

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            result.AddDiagnosticToAllMetrics(
                EvaluationDiagnostic.Error(
                    "Evaluation failed because the model response supplied for evaluation was null or empty."));

            return result;
        }

        (ChatMessage? userRequest, List<ChatMessage> history) = GetUserRequestAndHistory(messages);

        int inputTokenLimit = 0;
        int ignoredMessagesCount = 0;

        if (chatConfiguration.TokenCounter is not null)
        {
            IEvaluationTokenCounter tokenCounter = chatConfiguration.TokenCounter;
            inputTokenLimit = tokenCounter.InputTokenLimit;
            int tokenBudget = inputTokenLimit;

            void OnTokenBudgetExceeded()
            {
                EvaluationDiagnostic tokenBudgetExceeded =
                    EvaluationDiagnostic.Error(
                        $"Evaluation failed because the specified limit of {inputTokenLimit} input tokens was exceeded.");

                result.AddDiagnosticToAllMetrics(tokenBudgetExceeded);
            }

            if (!string.IsNullOrWhiteSpace(SystemPrompt))
            {
                tokenBudget -= tokenCounter.CountTokens(SystemPrompt!);
                if (tokenBudget < 0)
                {
                    OnTokenBudgetExceeded();
                    return result;
                }
            }

            string baseEvaluationPrompt =
                await RenderEvaluationPromptAsync(
                    userRequest,
                    modelResponse,
                    includedHistory: [],
                    additionalContext,
                    cancellationToken).ConfigureAwait(false);

            tokenBudget -= tokenCounter.CountTokens(baseEvaluationPrompt);
            if (tokenBudget < 0)
            {
                OnTokenBudgetExceeded();
                return result;
            }

            if (history.Count > 0 && !IgnoresHistory)
            {
                if (history.Count == 1)
                {
                    bool canRender =
                        await CanRenderAsync(
                            history[0],
                            ref tokenBudget,
                            chatConfiguration,
                            cancellationToken).ConfigureAwait(false);

                    if (!canRender)
                    {
                        ignoredMessagesCount = 1;
                        history = [];
                    }
                }
                else
                {
                    int totalMessagesCount = history.Count;
                    int includedMessagesCount = 0;

                    history.Reverse();

                    foreach (ChatMessage message in history)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        bool canRender =
                            await CanRenderAsync(
                                message,
                                ref tokenBudget,
                                chatConfiguration,
                                cancellationToken).ConfigureAwait(false);

                        if (!canRender)
                        {
                            ignoredMessagesCount = totalMessagesCount - includedMessagesCount;
                            history.RemoveRange(index: includedMessagesCount, count: ignoredMessagesCount);
                            break;
                        }

                        includedMessagesCount++;
                    }

                    history.Reverse();
                }
            }
        }

        var evaluationMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            evaluationMessages.Add(new ChatMessage(ChatRole.System, SystemPrompt!));
        }

        string evaluationPrompt =
            await RenderEvaluationPromptAsync(
                userRequest,
                modelResponse,
                includedHistory: history,
                additionalContext,
                cancellationToken).ConfigureAwait(false);

        evaluationMessages.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        ChatResponse evaluationResponse =
            await chatConfiguration.ChatClient.GetResponseAsync(
                evaluationMessages,
                ChatOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        string? evaluationResponseContent = evaluationResponse.Message.Text;

        if (string.IsNullOrWhiteSpace(evaluationResponseContent))
        {
            result.AddDiagnosticToAllMetrics(
                EvaluationDiagnostic.Error(
                    "Evaluation failed because the model failed to produce a valid evaluation response."));
        }
        else
        {
            await ParseEvaluationResponseAsync(
                evaluationResponseContent!,
                result,
                chatConfiguration,
                cancellationToken).ConfigureAwait(false);
        }

        if (inputTokenLimit > 0 && ignoredMessagesCount > 0)
        {
#pragma warning disable S103 // Lines should not be too long
            result.AddDiagnosticToAllMetrics(
                EvaluationDiagnostic.Warning(
                    $"The evaluation may be inconclusive because the oldest {ignoredMessagesCount} messages in the supplied conversation history were ignored in order to stay under the specified limit of {inputTokenLimit} input tokens."));
#pragma warning restore S103
        }

        return result;
    }

    /// <summary>
    /// Determines if there is sufficient <paramref name="tokenBudget"/> remaining to render the
    /// supplied <paramref name="message"/> as part of the evaluation prompt that this <see cref="IEvaluator"/> uses.
    /// </summary>
    /// <param name="message">
    /// A message that is part of the conversation history for the response being evaluated and that is to be rendered
    /// as part of the evaluation prompt.
    /// </param>
    /// <param name="tokenBudget">
    /// The remaining number of tokens available for the rendering additional content as part of the evaluation prompt.
    /// </param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that this <see cref="IEvaluator"/> uses to perform the evaluation.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if there is sufficient <paramref name="tokenBudget"/> remaining to render the supplied
    /// <paramref name="message"/> as part of the evaluation prompt; <see langword="false"/> otherwise.
    /// </returns>
    protected virtual ValueTask<bool> CanRenderAsync(
        ChatMessage message,
        ref int tokenBudget,
        ChatConfiguration chatConfiguration,
        CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(message, nameof(message));
        _ = Throw.IfNull(chatConfiguration, nameof(chatConfiguration));

        IEvaluationTokenCounter? tokenCounter = chatConfiguration.TokenCounter;
        if (tokenCounter is null)
        {
            return new ValueTask<bool>(true);
        }

        string? author = message.AuthorName;
        string role = message.Role.Value;
        string content = message.Text ?? string.Empty;

        int tokenCount =
            string.IsNullOrWhiteSpace(author)
                ? tokenCounter.CountTokens("[") +
                    tokenCounter.CountTokens(role) +
                    tokenCounter.CountTokens("] ") +
                    tokenCounter.CountTokens(content) +
                    tokenCounter.CountTokens("\n")
                : tokenCounter.CountTokens("[") +
                    tokenCounter.CountTokens(author!) +
                    tokenCounter.CountTokens(" (") +
                    tokenCounter.CountTokens(role) +
                    tokenCounter.CountTokens(")] ") +
                    tokenCounter.CountTokens(content) +
                    tokenCounter.CountTokens("\n");

        if (tokenCount > tokenBudget)
        {
            return new ValueTask<bool>(false);
        }
        else
        {
            tokenBudget -= tokenCount;
            return new ValueTask<bool>(true);
        }
    }

    /// <summary>
    /// Renders the supplied <paramref name="message"/> to a string that can be included as part of the evaluation
    /// prompt that this <see cref="IEvaluator"/> uses.
    /// </summary>
    /// <param name="message">
    /// A message that is part of the conversation history for the response being evaluated and that is to be rendered
    /// as part of the evaluation prompt.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// A string representation of the supplied <paramref name="message"/> that can be included as part of the
    /// evaluation prompt.
    /// </returns>
    protected virtual ValueTask<string> RenderAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(message, nameof(message));

        string? author = message.AuthorName;
        string role = message.Role.Value;
        string? content = message.Text;

        return string.IsNullOrWhiteSpace(author)
            ? new ValueTask<string>($"[{role}] {content}\n")
            : new ValueTask<string>($"[{author} ({role})] {content}\n");
    }

    /// <summary>
    /// Renders the information present in the supplied parameters into a prompt that this <see cref="IEvaluator"/>
    /// uses to perform the evaluation.
    /// </summary>
    /// <param name="userRequest">
    /// The request that produced the <paramref name="modelResponse"/> that is to be evaluated.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="includedHistory">
    /// The conversation history (excluding the <paramref name="userRequest"/> and <paramref name="modelResponse"/>)
    /// that is to be included as part of the evaluation prompt.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in the <paramref name="userRequest"/> and
    /// <paramref name="includedHistory"/>) that this <see cref="IEvaluator"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>The evaluation prompt.</returns>
    protected abstract ValueTask<string> RenderEvaluationPromptAsync(
        ChatMessage? userRequest,
        ChatMessage modelResponse,
        IEnumerable<ChatMessage>? includedHistory,
        IEnumerable<EvaluationContext>? additionalContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns an <see cref="EvaluationResult"/> that includes default values for all the
    /// <see cref="EvaluationMetric"/>s supported by this <see cref="IEvaluator"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="EvaluationMetricNames"/>.
    /// </remarks>
    /// <returns>
    /// An <see cref="EvaluationResult"/> that includes default values for all the
    /// <see cref="EvaluationMetric"/>s supported by this <see cref="IEvaluator"/>.
    /// </returns>
    protected abstract EvaluationResult InitializeResult();

    /// <summary>
    /// Parses the evaluation result present in <paramref name="modelResponseForEvaluationPrompt"/> into the
    /// <see cref="EvaluationMetric"/>s present in the supplied <paramref name="result"/>.
    /// </summary>
    /// <param name="modelResponseForEvaluationPrompt">
    /// An AI-generated response that contains the result of the current evaluation.
    /// </param>
    /// <param name="result">
    /// An <see cref="EvaluationResult"/> that includes a collection of <see cref="EvaluationMetric"/>s that are
    /// supported by this <see cref="IEvaluator"/>.
    /// </param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that this <see cref="IEvaluator"/> uses to perform the evaluation.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    protected abstract ValueTask ParseEvaluationResponseAsync(
        string modelResponseForEvaluationPrompt,
        EvaluationResult result,
        ChatConfiguration chatConfiguration,
        CancellationToken cancellationToken);

    private (ChatMessage? userRequest, List<ChatMessage> history) GetUserRequestAndHistory(
        IEnumerable<ChatMessage> messages)
    {
        ChatMessage? userRequest = null;
        List<ChatMessage> history;

        if (IgnoresHistory)
        {
            userRequest =
                messages.LastOrDefault() is ChatMessage lastMessage && lastMessage.Role == ChatRole.User
                    ? lastMessage
                    : null;

            history = [];
        }
        else
        {
            history = [.. messages];
            int lastMessageIndex = history.Count - 1;

            if (lastMessageIndex >= 0 &&
                history[lastMessageIndex] is ChatMessage lastMessage &&
                lastMessage.Role == ChatRole.User)
            {
                userRequest = lastMessage;
                history.RemoveAt(lastMessageIndex);
            }
        }

        return (userRequest, history);
    }
}

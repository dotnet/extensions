// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public virtual async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse);
        _ = Throw.IfNull(chatConfiguration);

        EvaluationResult result = InitializeResult();

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error(
                    "Evaluation failed because the model response supplied for evaluation was null or empty."));

            return result;
        }

        (ChatMessage? userRequest, List<ChatMessage> conversationHistory) =
            GetUserRequestAndConversationHistory(messages);

        var evaluationMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            evaluationMessages.Add(new ChatMessage(ChatRole.System, SystemPrompt!));
        }

        string evaluationPrompt =
            await RenderEvaluationPromptAsync(
                userRequest,
                modelResponse,
                conversationHistory,
                additionalContext,
                cancellationToken).ConfigureAwait(false);

        evaluationMessages.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        await PerformEvaluationAsync(
            chatConfiguration,
            evaluationMessages,
            result,
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Renders the supplied <paramref name="response"/> to a string that can be included as part of the evaluation
    /// prompt that this <see cref="IEvaluator"/> uses.
    /// </summary>
    /// <param name="response">
    /// Chat response being evaluated and that is to be rendered as part of the evaluation prompt.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// A string representation of the supplied <paramref name="response"/> that can be included as part of the
    /// evaluation prompt.
    /// </returns>
    /// <remarks>
    /// The default implementation uses <see cref="RenderAsync(ChatMessage, CancellationToken)"/> to render
    /// each message in the response.
    /// </remarks>
    protected virtual async ValueTask<string> RenderAsync(ChatResponse response, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(response);

        StringBuilder sb = new();
        foreach (ChatMessage message in response.Messages)
        {
            _ = sb.Append(await RenderAsync(message, cancellationToken).ConfigureAwait(false));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the supplied <paramref name="message"/> to a string that can be included as part of the evaluation
    /// prompt that this <see cref="IEvaluator"/> uses.
    /// </summary>
    /// <param name="message">
    /// Message that is part of the conversation history for the response being evaluated and that is to be rendered
    /// as part of the evaluation prompt.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// A string representation of the supplied <paramref name="message"/> that can be included as part of the
    /// evaluation prompt.
    /// </returns>
    protected virtual ValueTask<string> RenderAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(message);

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
    /// <param name="conversationHistory">
    /// The conversation history (excluding the <paramref name="userRequest"/> and <paramref name="modelResponse"/>)
    /// that is to be included as part of the evaluation prompt.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in the <paramref name="userRequest"/> and
    /// <paramref name="conversationHistory"/>) that this <see cref="IEvaluator"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>The evaluation prompt.</returns>
    protected abstract ValueTask<string> RenderEvaluationPromptAsync(
        ChatMessage? userRequest,
        ChatResponse modelResponse,
        IEnumerable<ChatMessage>? conversationHistory,
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
    /// Invokes the supplied <see cref="ChatConfiguration.ChatClient"/> with the supplied
    /// <paramref name="evaluationMessages"/> to perform the evaluation, and includes the results as one or more
    /// <see cref="EvaluationMetric"/>s in the supplied <paramref name="result"/>.
    /// </summary>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> that should be used if one or
    /// more composed <see cref="IEvaluator"/>s use an AI model to perform evaluation.
    /// </param>
    /// <param name="evaluationMessages">
    /// The set of messages that are to be sent to the supplied <see cref="ChatConfiguration.ChatClient"/> to perform
    /// the evaluation.
    /// </param>
    /// <param name="result">
    /// An <see cref="EvaluationResult"/> that includes a collection of <see cref="EvaluationMetric"/>s that are
    /// supported by this <see cref="IEvaluator"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    protected abstract ValueTask PerformEvaluationAsync(
        ChatConfiguration chatConfiguration,
        IList<ChatMessage> evaluationMessages,
        EvaluationResult result,
        CancellationToken cancellationToken);

    private (ChatMessage? userRequest, List<ChatMessage> conversationHistory) GetUserRequestAndConversationHistory(
        IEnumerable<ChatMessage> messages)
    {
        ChatMessage? userRequest = null;
        List<ChatMessage> conversationHistory;

        if (IgnoresHistory)
        {
            userRequest =
                messages.LastOrDefault() is ChatMessage lastMessage && lastMessage.Role == ChatRole.User
                    ? lastMessage
                    : null;

            conversationHistory = [];
        }
        else
        {
            conversationHistory = [.. messages];
            int lastMessageIndex = conversationHistory.Count - 1;

            if (lastMessageIndex >= 0 &&
                conversationHistory[lastMessageIndex] is ChatMessage lastMessage &&
                lastMessage.Role == ChatRole.User)
            {
                userRequest = lastMessage;
                conversationHistory.RemoveAt(lastMessageIndex);
            }
        }

        return (userRequest, conversationHistory);
    }
}

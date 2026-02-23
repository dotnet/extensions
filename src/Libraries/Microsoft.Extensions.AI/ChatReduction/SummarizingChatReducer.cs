// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides functionality to reduce a collection of chat messages into a summarized form.
/// </summary>
/// <remarks>
/// This reducer is useful for scenarios where it is necessary to constrain the size of a chat history,
/// such as when preparing input for models with context length limits. The reducer automatically summarizes
/// older messages when the conversation exceeds a specified length, preserving context while reducing message
/// count. The reducer maintains system messages and excludes messages containing function call or function
/// result content from summarization.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIChatReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class SummarizingChatReducer : IChatReducer
{
    private const string SummaryKey = "__summary__";

    private const string DefaultSummarizationPrompt = """
        **Generate a clear and complete summary of the entire conversation in no more than five sentences.**

        The summary must always:
        - Reflect contributions from both the user and the assistant
        - Preserve context to support ongoing dialogue
        - Incorporate any previously provided summary
        - Emphasize the most relevant and meaningful points

        The summary must never:
        - Offer critique, correction, interpretation, or speculation
        - Highlight errors, misunderstandings, or judgments of accuracy
        - Comment on events or ideas not present in the conversation
        - Omit any details included in an earlier summary
        """;

    private readonly IChatClient _chatClient;
    private readonly int _targetCount;
    private readonly int _thresholdCount;

    /// <summary>
    /// Gets or sets the prompt text used for summarization.
    /// </summary>
    public string SummarizationPrompt
    {
        get;
        set => field = Throw.IfNull(value);
    } = DefaultSummarizationPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummarizingChatReducer"/> class with the specified chat client,
    /// target count, and optional threshold count.
    /// </summary>
    /// <param name="chatClient">The chat client used to interact with the chat system. Cannot be <see langword="null"/>.</param>
    /// <param name="targetCount">The target number of messages to retain after summarization. Must be greater than 0.</param>
    /// <param name="threshold">The number of messages allowed beyond <paramref name="targetCount"/> before summarization is triggered. Must be greater than or equal to 0 if specified.</param>
    public SummarizingChatReducer(IChatClient chatClient, int targetCount, int? threshold)
    {
        _chatClient = Throw.IfNull(chatClient);
        _targetCount = Throw.IfLessThanOrEqual(targetCount, min: 0);
        _thresholdCount = Throw.IfLessThan(threshold ?? 0, min: 0, nameof(threshold));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(messages);

        var summarizedConversation = SummarizedConversation.FromChatMessages(messages);
        var indexOfFirstMessageToKeep = summarizedConversation.FindIndexOfFirstMessageToKeep(_targetCount, _thresholdCount);
        if (indexOfFirstMessageToKeep > 0)
        {
            summarizedConversation = await summarizedConversation.ResummarizeAsync(
                _chatClient,
                indexOfFirstMessageToKeep,
                SummarizationPrompt,
                cancellationToken);
        }

        return summarizedConversation.ToChatMessages();
    }

    /// <summary>Represents a conversation with an optional summary.</summary>
    private readonly struct SummarizedConversation(string? summary, ChatMessage? systemMessage, IList<ChatMessage> unsummarizedMessages)
    {
        /// <summary>Creates a <see cref="SummarizedConversation"/> from a list of chat messages.</summary>
        public static SummarizedConversation FromChatMessages(IEnumerable<ChatMessage> messages)
        {
            string? summary = null;
            ChatMessage? systemMessage = null;
            var unsummarizedMessages = new List<ChatMessage>();

            foreach (var message in messages)
            {
                if (message.Role == ChatRole.System)
                {
                    systemMessage ??= message;
                }
                else if (message.AdditionalProperties?.TryGetValue<string>(SummaryKey, out var summaryValue) == true)
                {
                    unsummarizedMessages.Clear();
                    summary = summaryValue;
                }
                else
                {
                    unsummarizedMessages.Add(message);
                }
            }

            return new(summary, systemMessage, unsummarizedMessages);
        }

        /// <summary>Performs summarization by calling the chat client and updating the conversation state.</summary>
        public async ValueTask<SummarizedConversation> ResummarizeAsync(
            IChatClient chatClient, int indexOfFirstMessageToKeep, string summarizationPrompt, CancellationToken cancellationToken)
        {
            Debug.Assert(indexOfFirstMessageToKeep > 0, "Expected positive index for first message to keep.");

            // Generate the summary by sending unsummarized messages to the chat client
            var summarizerChatMessages = ToSummarizerChatMessages(indexOfFirstMessageToKeep, summarizationPrompt);
            var response = await chatClient.GetResponseAsync(summarizerChatMessages, cancellationToken: cancellationToken);
            var newSummary = response.Text;

            // Attach the summary metadata to the last message being summarized
            // This is what allows us to build on previously-generated summaries
            var lastSummarizedMessage = unsummarizedMessages[indexOfFirstMessageToKeep - 1];
            var additionalProperties = lastSummarizedMessage.AdditionalProperties ??= [];
            additionalProperties[SummaryKey] = newSummary;

            // Compute the new list of unsummarized messages
            var newUnsummarizedMessages = unsummarizedMessages.Skip(indexOfFirstMessageToKeep).ToList();
            return new SummarizedConversation(newSummary, systemMessage, newUnsummarizedMessages);
        }

        /// <summary>Determines the index of the first message to keep (not summarize) based on target and threshold counts.</summary>
        public int FindIndexOfFirstMessageToKeep(int targetCount, int thresholdCount)
        {
            var earliestAllowedIndex = unsummarizedMessages.Count - thresholdCount - targetCount;
            if (earliestAllowedIndex <= 0)
            {
                // Not enough messages to warrant summarization
                return 0;
            }

            // Start at the ideal cut point (keeping exactly targetCount messages)
            var indexOfFirstMessageToKeep = unsummarizedMessages.Count - targetCount;

            // Move backward to skip over function call/result content at the boundary
            // We want to keep complete function call sequences together with their responses
            while (indexOfFirstMessageToKeep > 0)
            {
                if (!unsummarizedMessages[indexOfFirstMessageToKeep - 1].Contents.Any(IsToolRelatedContent))
                {
                    break;
                }

                indexOfFirstMessageToKeep--;
            }

            // Search backward within the threshold window to find a User message
            // If found, cut right before it to avoid orphaning user questions from responses
            for (var i = indexOfFirstMessageToKeep; i >= earliestAllowedIndex; i--)
            {
                if (unsummarizedMessages[i].Role == ChatRole.User)
                {
                    return i;
                }
            }

            // No User message found within threshold - use the adjusted cut point
            return indexOfFirstMessageToKeep;
        }

        /// <summary>Converts the summarized conversation back into a collection of chat messages.</summary>
        public IEnumerable<ChatMessage> ToChatMessages()
        {
            if (systemMessage is not null)
            {
                yield return systemMessage;
            }

            if (summary is not null)
            {
                yield return new ChatMessage(ChatRole.Assistant, summary);
            }

            foreach (var message in unsummarizedMessages)
            {
                yield return message;
            }
        }

        /// <summary>Returns whether the given <see cref="AIContent"/> relates to tool calling capabilities.</summary>
        /// <remarks>
        /// This method returns <see langword="true"/> for content types whose meaning depends on other related <see cref="AIContent"/> 
        /// instances in the conversation, such as function calls that require corresponding results, or other tool interactions that span 
        /// multiple messages. Such content should be kept together during summarization.
        /// </remarks>
        private static bool IsToolRelatedContent(AIContent content) => content
            is FunctionCallContent
            or FunctionResultContent
            or UserInputRequestContent
            or UserInputResponseContent;

        /// <summary>Builds the list of messages to send to the chat client for summarization.</summary>
        private IEnumerable<ChatMessage> ToSummarizerChatMessages(int indexOfFirstMessageToKeep, string summarizationPrompt)
        {
            if (summary is not null)
            {
                yield return new ChatMessage(ChatRole.Assistant, summary);
            }

            for (var i = 0; i < indexOfFirstMessageToKeep; i++)
            {
                var message = unsummarizedMessages[i];
                if (!message.Contents.Any(IsToolRelatedContent))
                {
                    yield return message;
                }
            }

            yield return new ChatMessage(ChatRole.System, summarizationPrompt);
        }
    }
}

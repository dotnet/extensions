// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides functionality to reduce a collection of chat messages into a summarized form.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IChatReducer"/> interface to process and summarize chat
/// messages.
/// </remarks>
[Experimental("MEAI001")]
public sealed class SummarizingChatReducer : IChatReducer
{
    private const string SummaryKey = "__summary__";

    private const string SummarizerSystemPrompt = """
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
    /// Initializes a new instance of the <see cref="SummarizingChatReducer"/> class with the specified chat client,
    /// target count, and optional threshold count.
    /// </summary>
    /// <param name="chatClient">The chat client used to interact with the chat system. Cannot be <see langword="null"/>.</param>
    /// <param name="targetCount">The target number of messages to retain after summarization. Must be greater than 0.</param>
    /// <param name="threshold">The number of messages allowed beyond <paramref name="targetCount"/> before summarization is triggered. Must be greater than or equal to 0 if specified.</param>
    public SummarizingChatReducer(IChatClient chatClient, int targetCount, int? threshold)
    {
        _chatClient = Throw.IfNull(chatClient);
        _targetCount = Throw.IfLessThanOrEqual(targetCount, 0);
        _thresholdCount = Throw.IfLessThan(threshold ?? 0, min: 0, nameof(threshold));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(messages);

        var summarizedConversion = SummarizedConversation.FromChatMessages(messages);
        if (summarizedConversion.ShouldResummarize(_targetCount, _thresholdCount))
        {
            summarizedConversion = await summarizedConversion.ResummarizeAsync(_chatClient, _targetCount, cancellationToken);
        }

        return summarizedConversion.ToChatMessages();
    }

    private readonly struct SummarizedConversation(string? summary, ChatMessage? systemMessage, IList<ChatMessage> unsummarizedMessages)
    {
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
                else if (!message.Contents.Any(m => m is FunctionCallContent or FunctionResultContent))
                {
                    unsummarizedMessages.Add(message);
                }
            }

            return new(summary, systemMessage, unsummarizedMessages);
        }

        public bool ShouldResummarize(int targetCount, int thresholdCount)
            => unsummarizedMessages.Count > targetCount + thresholdCount;

        public async Task<SummarizedConversation> ResummarizeAsync(
            IChatClient chatClient, int targetCount, CancellationToken cancellationToken)
        {
            var messagesToResummarize = unsummarizedMessages.Count - targetCount;
            if (messagesToResummarize <= 0)
            {
                // We're at or below the target count - no need to resummarize.
                return this;
            }

            var summarizerChatMessages = ToResummarizerChatMessages(messagesToResummarize);
            var response = await chatClient.GetResponseAsync(summarizerChatMessages, cancellationToken: cancellationToken);
            var newSummary = response.Text;

            var lastSummarizedMessage = unsummarizedMessages[messagesToResummarize - 1];
            var additionalProperties = lastSummarizedMessage.AdditionalProperties ??= [];
            additionalProperties[SummaryKey] = newSummary;

            var newUnsummarizedMessages = unsummarizedMessages.Skip(messagesToResummarize).ToList();
            return new SummarizedConversation(newSummary, systemMessage, newUnsummarizedMessages);
        }

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

        private IEnumerable<ChatMessage> ToResummarizerChatMessages(int messagesToResummarize)
        {
            if (summary is not null)
            {
                yield return new ChatMessage(ChatRole.Assistant, summary);
            }

            for (var i = 0; i < messagesToResummarize; i++)
            {
                yield return unsummarizedMessages[i];
            }

            yield return new ChatMessage(ChatRole.System, SummarizerSystemPrompt);
        }
    }
}

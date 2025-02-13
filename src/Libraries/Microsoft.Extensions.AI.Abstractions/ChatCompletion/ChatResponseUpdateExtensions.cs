// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
#if NET
using System.Runtime.InteropServices;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S127 // "for" loop stop conditions should be invariant
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="ChatResponseUpdate"/> instances.
/// </summary>
public static class ChatResponseUpdateExtensions
{
    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="ChatMessage"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    public static ChatResponse ToChatResponse(
        this IEnumerable<ChatResponseUpdate> updates, bool coalesceContent = true)
    {
        _ = Throw.IfNull(updates);

        ChatResponse response = new([]);
        Dictionary<int, ChatMessage> messages = [];

        foreach (var update in updates)
        {
            ProcessUpdate(update, messages, response);
        }

        AddMessagesToResponse(messages, response, coalesceContent);

        return response;
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="ChatMessage"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    public static Task<ChatResponse> ToChatResponseAsync(
        this IAsyncEnumerable<ChatResponseUpdate> updates, bool coalesceContent = true, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToChatResponseAsync(updates, coalesceContent, cancellationToken);

        static async Task<ChatResponse> ToChatResponseAsync(
            IAsyncEnumerable<ChatResponseUpdate> updates, bool coalesceContent, CancellationToken cancellationToken)
        {
            ChatResponse response = new([]);
            Dictionary<int, ChatMessage> messages = [];

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, messages, response);
            }

            AddMessagesToResponse(messages, response, coalesceContent);

            return response;
        }
    }

    /// <summary>Processes the <see cref="ChatResponseUpdate"/>, incorporating its contents into <paramref name="messages"/> and <paramref name="response"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="messages">The dictionary mapping <see cref="ChatResponseUpdate.ChoiceIndex"/> to the <see cref="ChatMessage"/> being built for that choice.</param>
    /// <param name="response">The <see cref="ChatResponse"/> object whose properties should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(ChatResponseUpdate update, Dictionary<int, ChatMessage> messages, ChatResponse response)
    {
        response.ResponseId ??= update.ResponseId;
        response.CreatedAt ??= update.CreatedAt;
        response.FinishReason ??= update.FinishReason;
        response.ModelId ??= update.ModelId;

#if NET
        ChatMessage message = CollectionsMarshal.GetValueRefOrAddDefault(messages, update.ChoiceIndex, out _) ??=
            new(default, new List<AIContent>());
#else
        if (!messages.TryGetValue(update.ChoiceIndex, out ChatMessage? message))
        {
            messages[update.ChoiceIndex] = message = new(default, new List<AIContent>());
        }
#endif

        // Incorporate all content from the update into the response.
        foreach (var content in update.Contents)
        {
            switch (content)
            {
                // Usage content is treated specially and propagated to the response's Usage.
                case UsageContent usage:
                    (response.Usage ??= new()).Add(usage.Details);
                    break;

                default:
                    message.Contents.Add(content);
                    break;
            }
        }

        message.AuthorName ??= update.AuthorName;
        if (update.Role is ChatRole role && message.Role == default)
        {
            message.Role = role;
        }

        if (update.AdditionalProperties is not null)
        {
            if (message.AdditionalProperties is null)
            {
                message.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                foreach (var entry in update.AdditionalProperties)
                {
                    // Use first-wins behavior to match the behavior of the other properties.
                    _ = message.AdditionalProperties.TryAdd(entry.Key, entry.Value);
                }
            }
        }
    }

    /// <summary>Finalizes the <paramref name="response"/> object by transferring the <paramref name="messages"/> into it.</summary>
    /// <param name="messages">The messages to process further and transfer into <paramref name="response"/>.</param>
    /// <param name="response">The result <see cref="ChatResponse"/> being built.</param>
    /// <param name="coalesceContent">The corresponding option value provided to <see cref="ToChatResponse"/> or <see cref="ToChatResponseAsync"/>.</param>
    private static void AddMessagesToResponse(Dictionary<int, ChatMessage> messages, ChatResponse response, bool coalesceContent)
    {
        if (messages.Count <= 1)
        {
            // Add the single message if there is one.
            foreach (var entry in messages)
            {
                AddMessage(response, coalesceContent, entry);
            }

            // In the vast majority case where there's only one choice, promote any additional properties
            // from the single message to the chat response, making them more discoverable and more similar
            // to how they're typically surfaced from non-streaming services.
            if (response.Choices.Count == 1 &&
                response.Choices[0].AdditionalProperties is { } messageProps)
            {
                response.Choices[0].AdditionalProperties = null;
                response.AdditionalProperties = messageProps;
            }
        }
        else
        {
            // Add all of the messages, sorted by choice index.
            foreach (var entry in messages.OrderBy(entry => entry.Key))
            {
                AddMessage(response, coalesceContent, entry);
            }

            // If there are multiple choices, we don't promote additional properties from the individual messages.
            // At a minimum, we'd want to know which choice the additional properties applied to, and if there were
            // conflicting values across the choices, it would be unclear which one should be used.
        }

        static void AddMessage(ChatResponse response, bool coalesceContent, KeyValuePair<int, ChatMessage> entry)
        {
            if (entry.Value.Role == default)
            {
                entry.Value.Role = ChatRole.Assistant;
            }

            if (coalesceContent)
            {
                CoalesceTextContent((List<AIContent>)entry.Value.Contents);
            }

            response.Choices.Add(entry.Value);
        }
    }

    /// <summary>Coalesces sequential <see cref="TextContent"/> content elements.</summary>
    private static void CoalesceTextContent(List<AIContent> contents)
    {
        StringBuilder? coalescedText = null;

        // Iterate through all of the items in the list looking for contiguous items that can be coalesced.
        int start = 0;
        while (start < contents.Count - 1)
        {
            // We need at least two TextContents in a row to be able to coalesce.
            if (contents[start] is not TextContent firstText)
            {
                start++;
                continue;
            }

            if (contents[start + 1] is not TextContent secondText)
            {
                start += 2;
                continue;
            }

            // Append the text from those nodes and continue appending subsequent TextContents until we run out.
            // We null out nodes as their text is appended so that we can later remove them all in one O(N) operation.
            coalescedText ??= new();
            _ = coalescedText.Clear().Append(firstText.Text).Append(secondText.Text);
            contents[start + 1] = null!;
            int i = start + 2;
            for (; i < contents.Count && contents[i] is TextContent next; i++)
            {
                _ = coalescedText.Append(next.Text);
                contents[i] = null!;
            }

            // Store the replacement node.
            contents[start] = new TextContent(coalescedText.ToString())
            {
                // We inherit the properties of the first text node. We don't currently propagate additional
                // properties from the subsequent nodes. If we ever need to, we can add that here.
                AdditionalProperties = firstText.AdditionalProperties?.Clone(),
            };

            start = i;
        }

        // Remove all of the null slots left over from the coalescing process.
        _ = contents.RemoveAll(u => u is null);
    }
}

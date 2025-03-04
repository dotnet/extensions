// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="ChatResponseUpdate"/> instances.
/// </summary>
public static class ChatResponseUpdateExtensions
{
    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="chatMessages"/>.</summary>
    /// <param name="chatMessages">The list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessages"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, tne
    /// method may use <see cref="ChatResponseUpdate.ResponseId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static void AddRangeFromUpdates(this IList<ChatMessage> chatMessages, IEnumerable<ChatResponseUpdate> updates)
    {
        _ = Throw.IfNull(chatMessages);
        _ = Throw.IfNull(updates);

        if (updates is ICollection<ChatResponseUpdate> { Count: 0 })
        {
            return;
        }

        ChatResponse response = updates.ToChatResponse();
        if (chatMessages is List<ChatMessage> list)
        {
            list.AddRange(response.Messages);
        }
        else
        {
            int count = response.Messages.Count;
            for (int i = 0; i < count; i++)
            {
                chatMessages.Add(response.Messages[i]);
            }
        }
    }

    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="chatMessages"/>.</summary>
    /// <param name="chatMessages">The list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessages"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, tne
    /// method may use <see cref="ChatResponseUpdate.ResponseId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static Task AddRangeFromUpdatesAsync(
        this IList<ChatMessage> chatMessages, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        _ = Throw.IfNull(updates);

        return AddRangeFromUpdatesAsync(chatMessages, updates, cancellationToken);

        static async Task AddRangeFromUpdatesAsync(
            IList<ChatMessage> chatMessages, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken)
        {
            ChatResponse response = await updates.ToChatResponseAsync(cancellationToken).ConfigureAwait(false);
            if (chatMessages is List<ChatMessage> list)
            {
                list.AddRange(response.Messages);
            }
            else
            {
                int count = response.Messages.Count;
                for (int i = 0; i < count; i++)
                {
                    chatMessages.Add(response.Messages[i]);
                }
            }
        }
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a single <see cref="ChatResponse"/>, the method will attempt to reconstruct
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.ResponseId"/> to determine
    /// message boundaries, as well as coalescing contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static ChatResponse ToChatResponse(
        this IEnumerable<ChatResponseUpdate> updates)
    {
        _ = Throw.IfNull(updates);

        ChatResponse response = new();

        foreach (var update in updates)
        {
            ProcessUpdate(update, response);
        }

        FinalizeResponse(response);

        return response;
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a single <see cref="ChatResponse"/>, the method will attempt to reconstruct
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.ResponseId"/> to determine
    /// message boundaries, as well as coalescing contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static Task<ChatResponse> ToChatResponseAsync(
        this IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToChatResponseAsync(updates, cancellationToken);

        static async Task<ChatResponse> ToChatResponseAsync(
            IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken)
        {
            ChatResponse response = new();

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, response);
            }

            FinalizeResponse(response);

            return response;
        }
    }

    /// <summary>Finalizes the <paramref name="response"/> object.</summary>
    private static void FinalizeResponse(ChatResponse response)
    {
        int count = response.Messages.Count;
        for (int i = 0; i < count; i++)
        {
            CoalesceTextContent((List<AIContent>)response.Messages[i].Contents);
        }
    }

    /// <summary>Processes the <see cref="ChatResponseUpdate"/>, incorporating its contents into <paramref name="response"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="response">The <see cref="ChatResponse"/> object that should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(ChatResponseUpdate update, ChatResponse response)
    {
        // If there is no message created yet, or if the last update we saw had a different
        // response ID than the newest update, create a new message.
        ChatMessage message;
        if (response.Messages.Count == 0 ||
            (update.ResponseId is string updateId && response.ResponseId is string responseId && updateId != responseId))
        {
            message = new ChatMessage(ChatRole.Assistant, []);
            response.Messages.Add(message);
        }
        else
        {
            message = response.Messages[response.Messages.Count - 1];
        }

        // Some members on ChatResponseUpdate map to members of ChatMessage.
        // Incorporate those into the latest message; in cases where the message
        // stores a single value, prefer the latest update's value over anything
        // stored in the message.
        if (update.AuthorName is not null)
        {
            message.AuthorName = update.AuthorName;
        }

        if (update.Role is ChatRole role)
        {
            message.Role = role;
        }

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

        // Other members on a ChatResponseUpdate map to members of the ChatResponse.
        // Update the response object with those, preferring the values from later updates.

        if (update.ResponseId is not null)
        {
            // Note that this must come after the message checks earlier, as they depend
            // on this value for change detection.
            response.ResponseId = update.ResponseId;
        }

        if (update.ChatThreadId is not null)
        {
            response.ChatThreadId = update.ChatThreadId;
        }

        if (update.CreatedAt is not null)
        {
            response.CreatedAt = update.CreatedAt;
        }

        if (update.FinishReason is not null)
        {
            response.FinishReason = update.FinishReason;
        }

        if (update.ModelId is not null)
        {
            response.ModelId = update.ModelId;
        }

        if (update.AdditionalProperties is not null)
        {
            if (response.AdditionalProperties is null)
            {
                response.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                response.AdditionalProperties.SetAll(update.AdditionalProperties);
            }
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

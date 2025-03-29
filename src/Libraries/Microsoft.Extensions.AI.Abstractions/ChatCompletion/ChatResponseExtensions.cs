// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/> instances.
/// </summary>
public static class ChatResponseExtensions
{
    /// <summary>Adds all of the messages from <paramref name="response"/> into <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the messages from <paramref name="response"/> should be added.</param>
    /// <param name="response">The response containing the messages to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static void AddMessages(this IList<ChatMessage> list, ChatResponse response)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(response);

        if (list is List<ChatMessage> listConcrete)
        {
            listConcrete.AddRange(response.Messages);
        }
        else
        {
            foreach (var message in response.Messages)
            {
                list.Add(message);
            }
        }
    }

    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, the
    /// method may use <see cref="ChatResponseUpdate.MessageId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static void AddMessages(this IList<ChatMessage> list, IEnumerable<ChatResponseUpdate> updates)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(updates);

        if (updates is ICollection<ChatResponseUpdate> { Count: 0 })
        {
            return;
        }

        list.AddMessages(updates.ToChatResponse());
    }

    /// <summary>Converts the <paramref name="update"/> into a <see cref="ChatMessage"/> instance and adds it to <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the newly constructed message should be added.</param>
    /// <param name="update">The <see cref="ChatResponseUpdate"/> instance to convert to a message and add to the list.</param>
    /// <param name="filter">A predicate to filter which <see cref="AIContent"/> gets included in the message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If the <see cref="ChatResponseUpdate"/> has no content, or all its content gets excluded by <paramref name="filter"/>, then
    /// no <see cref="ChatMessage"/> will be added to the <paramref name="list"/>.
    /// </remarks>
    public static void AddMessages(this IList<ChatMessage> list, ChatResponseUpdate update, Func<AIContent, bool>? filter = null)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(update);

        var contentsList = filter is null ? update.Contents : update.Contents.Where(filter).ToList();
        if (contentsList.Count > 0)
        {
            list.Add(new ChatMessage(update.Role ?? ChatRole.Assistant, contentsList)
            {
                AuthorName = update.AuthorName,
                RawRepresentation = update.RawRepresentation,
                AdditionalProperties = update.AdditionalProperties,
            });
        }
    }

    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="list"/>.</summary>
    /// <param name="list">The list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, tne
    /// method may use <see cref="ChatResponseUpdate.MessageId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static Task AddMessagesAsync(
        this IList<ChatMessage> list, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(updates);

        return AddMessagesAsync(list, updates, cancellationToken);

        static async Task AddMessagesAsync(
            IList<ChatMessage> list, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken) =>
            list.AddMessages(await updates.ToChatResponseAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a single <see cref="ChatResponse"/>, the method will attempt to reconstruct
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.MessageId"/> to determine
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
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.MessageId"/> to determine
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

    /// <summary>Coalesces sequential <see cref="TextContent"/> content elements.</summary>
    internal static void CoalesceTextContent(List<AIContent> contents)
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
        // message ID than the newest update, create a new message.
        ChatMessage message;
        var isNewMessage = false;
        if (response.Messages.Count == 0)
        {
            isNewMessage = true;
        }
        else if (update.MessageId is { Length: > 0 } updateMessageId
            && response.Messages[response.Messages.Count - 1].MessageId is string lastMessageId
            && updateMessageId != lastMessageId)
        {
            isNewMessage = true;
        }

        if (isNewMessage)
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

        if (update.MessageId is { Length: > 0 })
        {
            // Note that this must come after the message checks earlier, as they depend
            // on this value for change detection.
            message.MessageId = update.MessageId;
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

        if (update.ResponseId is { Length: > 0 })
        {
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
}

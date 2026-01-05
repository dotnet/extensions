// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
#if !NET
using System.Runtime.InteropServices;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

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
            list.Add(new(update.Role ?? ChatRole.Assistant, contentsList)
            {
                AuthorName = update.AuthorName,
                CreatedAt = update.CreatedAt,
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

    /// <summary>
    /// Coalesces image result content elements in the provided list of <see cref="AIContent"/> items.
    /// Unlike other content coalescing methods, this will coalesce non-sequential items based on their Name property, 
    /// and it will replace earlier items with later ones when duplicates are found.
    /// </summary>
    private static void CoalesceImageResultContent(IList<AIContent> contents)
    {
        Dictionary<string, int>? imageResultIndexById = null;
        bool hasRemovals = false;

        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] is ImageGenerationToolResultContent imageResult && !string.IsNullOrEmpty(imageResult.ImageId))
            {
                // Check if there's an existing ImageGenerationToolResultContent with the same ImageId to replace
                if (imageResultIndexById is null)
                {
                    imageResultIndexById = new(StringComparer.Ordinal);
                }

                if (imageResultIndexById.TryGetValue(imageResult.ImageId!, out int existingIndex))
                {
                    // Replace the existing imageResult with the new one
                    contents[existingIndex] = imageResult;
                    contents[i] = null!; // Mark the current one for removal, then remove in single o(n) pass
                    hasRemovals = true;
                }
                else
                {
                    imageResultIndexById[imageResult.ImageId!] = i;
                }
            }
        }

        // Remove all of the null slots left over from the coalescing process.
        if (hasRemovals)
        {
            RemoveNullContents(contents);
        }
    }

    /// <summary>Coalesces sequential <see cref="AIContent"/> content elements.</summary>
    internal static void CoalesceContent(IList<AIContent> contents)
    {
        Coalesce<TextContent>(
            contents,
            mergeSingle: false,
            canMerge: null,
            static (contents, start, end) => new(MergeText(contents, start, end)) { AdditionalProperties = contents[start].AdditionalProperties?.Clone() });

        Coalesce<TextReasoningContent>(
            contents,
            mergeSingle: false,
            canMerge: static (r1, r2) => string.IsNullOrEmpty(r1.ProtectedData), // we allow merging if the first item has no ProtectedData, even if the second does
            static (contents, start, end) =>
            {
                TextReasoningContent content = new(MergeText(contents, start, end))
                {
                    AdditionalProperties = contents[start].AdditionalProperties?.Clone()
                };

#if DEBUG
                for (int i = start; i < end - 1; i++)
                {
                    Debug.Assert(contents[i] is TextReasoningContent { ProtectedData: null }, "Expected all but the last to have a null ProtectedData");
                }
#endif

                if (((TextReasoningContent)contents[end - 1]).ProtectedData is { } protectedData)
                {
                    content.ProtectedData = protectedData;
                }

                return content;
            });

        CoalesceImageResultContent(contents);

        Coalesce<DataContent>(
            contents,
            mergeSingle: false,
            canMerge: static (r1, r2) => r1.MediaType == r2.MediaType && r1.HasTopLevelMediaType("text") && r1.Name == r2.Name,
            static (contents, start, end) =>
            {
                Debug.Assert(end - start > 1, "Expected multiple contents to merge");

                MemoryStream ms = new();
                for (int i = start; i < end; i++)
                {
                    var current = (DataContent)contents[i];
#if NET
                    ms.Write(current.Data.Span);
#else
                    if (!MemoryMarshal.TryGetArray(current.Data, out var segment))
                    {
                        segment = new(current.Data.ToArray());
                    }

                    ms.Write(segment.Array!, segment.Offset, segment.Count);
#endif
                }

                var first = (DataContent)contents[start];
                return new DataContent(new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length), first.MediaType) { Name = first.Name };
            });

        Coalesce<CodeInterpreterToolCallContent>(
            contents,
            mergeSingle: true,
            canMerge: static (r1, r2) => r1.CallId == r2.CallId,
            static (contents, start, end) =>
            {
                var firstContent = (CodeInterpreterToolCallContent)contents[start];

                if (start == end - 1)
                {
                    if (firstContent.Inputs is not null)
                    {
                        CoalesceContent(firstContent.Inputs);
                    }

                    return firstContent;
                }

                List<AIContent>? inputs = null;

                for (int i = start; i < end; i++)
                {
                    (inputs ??= []).AddRange(((CodeInterpreterToolCallContent)contents[i]).Inputs ?? []);
                }

                if (inputs is not null)
                {
                    CoalesceContent(inputs);
                }

                return new()
                {
                    CallId = firstContent.CallId,
                    Inputs = inputs,
                    AdditionalProperties = firstContent.AdditionalProperties?.Clone(),
                };
            });

        Coalesce<CodeInterpreterToolResultContent>(
            contents,
            mergeSingle: true,
            canMerge: static (r1, r2) => r1.CallId is not null && r2.CallId is not null && r1.CallId == r2.CallId,
            static (contents, start, end) =>
            {
                var firstContent = (CodeInterpreterToolResultContent)contents[start];

                if (start == end - 1)
                {
                    if (firstContent.Outputs is not null)
                    {
                        CoalesceContent(firstContent.Outputs);
                    }

                    return firstContent;
                }

                List<AIContent>? output = null;

                for (int i = start; i < end; i++)
                {
                    (output ??= []).AddRange(((CodeInterpreterToolResultContent)contents[i]).Outputs ?? []);
                }

                if (output is not null)
                {
                    CoalesceContent(output);
                }

                return new()
                {
                    CallId = firstContent.CallId,
                    Outputs = output,
                    AdditionalProperties = firstContent.AdditionalProperties?.Clone(),
                };
            });

        static string MergeText(IList<AIContent> contents, int start, int end)
        {
            Debug.Assert(end - start > 1, "Expected multiple contents to merge");

            StringBuilder sb = new();
            for (int i = start; i < end; i++)
            {
                _ = sb.Append(contents[i]);
            }

            return sb.ToString();
        }

        static void Coalesce<TContent>(
            IList<AIContent> contents,
            bool mergeSingle,
            Func<TContent, TContent, bool>? canMerge,
            Func<IList<AIContent>, int, int, TContent> merge)
            where TContent : AIContent
        {
            // Iterate through all of the items in the list looking for contiguous items that can be coalesced.
            int start = 0;
            while (start < contents.Count)
            {
                if (!TryAsCoalescable(contents[start], out var firstContent))
                {
                    start++;
                    continue;
                }

                // Iterate until we find a non-coalescable item.
                int i = start + 1;
                TContent prev = firstContent;
                while (i < contents.Count && TryAsCoalescable(contents[i], out TContent? next) && (canMerge is null || canMerge(prev, next)))
                {
                    i++;
                    prev = next;
                }

                // If there's only one item in the run, and we don't want to merge single items, skip it.
                if (start == i - 1 && !mergeSingle)
                {
                    start++;
                    continue;
                }

                // Store the replacement node and null out all of the nodes that we coalesced.
                // We can then remove all coalesced nodes in one O(N) operation via RemoveAll.
                // Leave start positioned at the start of the next run.
                contents[start] = merge(contents, start, i);

                start++;
                while (start < i)
                {
                    contents[start++] = null!;
                }

                static bool TryAsCoalescable(AIContent content, [NotNullWhen(true)] out TContent? coalescable)
                {
                    if (content is TContent tmp && tmp.Annotations is not { Count: > 0 })
                    {
                        coalescable = tmp;
                        return true;
                    }

                    coalescable = null;
                    return false;
                }
            }

            // Remove all of the null slots left over from the coalescing process.
            RemoveNullContents(contents);
        }
    }

    private static void RemoveNullContents<T>(IList<T> contents)
        where T : class
    {
        if (contents is List<AIContent> contentsList)
        {
            _ = contentsList.RemoveAll(u => u is null);
        }
        else
        {
            int nextSlot = 0;
            int contentsCount = contents.Count;
            for (int i = 0; i < contentsCount; i++)
            {
                if (contents[i] is { } content)
                {
                    contents[nextSlot++] = content;
                }
            }

            for (int i = contentsCount - 1; i >= nextSlot; i--)
            {
                contents.RemoveAt(i);
            }

            Debug.Assert(nextSlot == contents.Count, "Expected final count to equal list length.");
        }
    }

    /// <summary>Finalizes the <paramref name="response"/> object.</summary>
    private static void FinalizeResponse(ChatResponse response)
    {
        int count = response.Messages.Count;
        for (int i = 0; i < count; i++)
        {
            CoalesceContent((List<AIContent>)response.Messages[i].Contents);
        }
    }

    /// <summary>Processes the <see cref="ChatResponseUpdate"/>, incorporating its contents into <paramref name="response"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="response">The <see cref="ChatResponse"/> object that should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(ChatResponseUpdate update, ChatResponse response)
    {
        // If there is no message created yet, or if the last update we saw had a different
        // identifying parts, create a new message.
        bool isNewMessage = true;
        if (response.Messages.Count != 0)
        {
            var lastMessage = response.Messages[response.Messages.Count - 1];
            isNewMessage =
                NotEmptyOrEqual(update.AuthorName, lastMessage.AuthorName) ||
                NotEmptyOrEqual(update.MessageId, lastMessage.MessageId) ||
                NotNullOrEqual(update.Role, lastMessage.Role);
        }

        // Get the message to target, either a new one or the last ones.
        ChatMessage message;
        if (isNewMessage)
        {
            message = new(ChatRole.Assistant, []);
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

        if (message.CreatedAt is null || (update.CreatedAt is not null && update.CreatedAt > message.CreatedAt))
        {
            message.CreatedAt = update.CreatedAt;
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

        // AdditionalProperties are scoped to the message if the update has a MessageId,
        // otherwise they're scoped to the response.
        if (update.AdditionalProperties is not null)
        {
            if (update.MessageId is { Length: > 0 })
            {
                if (message.AdditionalProperties is null)
                {
                    message.AdditionalProperties = new(update.AdditionalProperties);
                }
                else
                {
                    message.AdditionalProperties.SetAll(update.AdditionalProperties);
                }
            }
            else
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

        if (update.ConversationId is not null)
        {
            response.ConversationId = update.ConversationId;
        }

        if (response.CreatedAt is null || (update.CreatedAt is not null && update.CreatedAt > response.CreatedAt))
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
    }

    /// <summary>Gets whether both strings are not null/empty and not the same as each other.</summary>
    private static bool NotEmptyOrEqual(string? s1, string? s2) =>
        s1 is { Length: > 0 } str1 && s2 is { Length: > 0 } str2 && str1 != str2;

    /// <summary>Gets whether two roles are not null and not the same as each other.</summary>
    private static bool NotNullOrEqual(ChatRole? r1, ChatRole? r2) =>
        r1.HasValue && r2.HasValue && r1.Value != r2.Value;
}

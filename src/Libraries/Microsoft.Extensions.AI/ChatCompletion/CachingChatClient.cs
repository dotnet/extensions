// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S127 // "for" loop stop conditions should be invariant

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that caches the results of chat calls.
/// </summary>
public abstract class CachingChatClient : DelegatingChatClient
{
    /// <summary>Initializes a new instance of the <see cref="CachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    protected CachingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <summary>Gets or sets a value indicating whether to coalesce streaming updates.</summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the client will attempt to coalesce contiguous streaming updates
    /// into a single update, in order to reduce the number of individual items that are yielded on
    /// subsequent enumerations of the cached data. When <see langword="false"/>, the updates are
    /// kept unaltered.
    /// </para>
    /// <para>
    /// The default is <see langword="true"/>.
    /// </para>
    /// </remarks>
    public bool CoalesceStreamingUpdates { get; set; } = true;

    /// <inheritdoc />
    public override async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
        // or having problems when one of the callers cancels but others don't. This has the drawback that
        // concurrent callers might trigger duplicate requests, but that's acceptable.
        var cacheKey = GetCacheKey(false, chatMessages, options);

        if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is ChatCompletion existing)
        {
            return existing;
        }

        var result = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
        await WriteCacheAsync(cacheKey, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        var cacheKey = GetCacheKey(true, chatMessages, options);
        if (await ReadCacheStreamingAsync(cacheKey, cancellationToken).ConfigureAwait(false) is { } existingChunks)
        {
            // Yield all of the cached items.
            foreach (var chunk in existingChunks)
            {
                yield return chunk;
            }
        }
        else
        {
            // Yield and store all of the items.
            List<StreamingChatCompletionUpdate> capturedItems = [];
            await foreach (var chunk in base.CompleteStreamingAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
            {
                capturedItems.Add(chunk);
                yield return chunk;
            }

            // If the caching client is configured to coalesce streaming updates, do so now within the capturedItems list.
            if (CoalesceStreamingUpdates)
            {
                StringBuilder coalescedText = new();

                // Iterate through all of the items in the list looking for contiguous items that can be coalesced.
                for (int startInclusive = 0; startInclusive < capturedItems.Count; startInclusive++)
                {
                    // If an item isn't generally coalescable, skip it.
                    StreamingChatCompletionUpdate update = capturedItems[startInclusive];
                    if (update.ChoiceIndex != 0 ||
                        update.Contents.Count != 1 ||
                        update.Contents[0] is not TextContent textContent)
                    {
                        continue;
                    }

                    // We found a coalescable item. Look for more contiguous items that are also coalescable with it.
                    int endExclusive = startInclusive + 1;
                    for (; endExclusive < capturedItems.Count; endExclusive++)
                    {
                        StreamingChatCompletionUpdate next = capturedItems[endExclusive];
                        if (next.ChoiceIndex != 0 ||
                            next.Contents.Count != 1 ||
                            next.Contents[0] is not TextContent ||

                            // changing role or author would be really strange, but check anyway
                            (update.Role is not null && next.Role is not null && update.Role != next.Role) ||
                            (update.AuthorName is not null && next.AuthorName is not null && update.AuthorName != next.AuthorName))
                        {
                            break;
                        }
                    }

                    // If we couldn't find anything to coalesce, there's nothing to do.
                    if (endExclusive - startInclusive <= 1)
                    {
                        continue;
                    }

                    // We found a coalescable run of items. Create a new node to represent the run. We create a new one
                    // rather than reappropriating one of the existing ones so as not to mutate an item already yielded.
                    _ = coalescedText.Clear().Append(capturedItems[startInclusive].Text);

                    TextContent coalescedContent = new(null) // will patch the text after examining all items in the run
                    {
                        AdditionalProperties = textContent.AdditionalProperties?.Clone(),
                    };

                    StreamingChatCompletionUpdate coalesced = new()
                    {
                        AdditionalProperties = update.AdditionalProperties?.Clone(),
                        AuthorName = update.AuthorName,
                        CompletionId = update.CompletionId,
                        Contents = [coalescedContent],
                        CreatedAt = update.CreatedAt,
                        FinishReason = update.FinishReason,
                        ModelId = update.ModelId,
                        Role = update.Role,

                        // Explicitly don't include RawRepresentation. It's not applicable if one update ends up being used
                        // to represent multiple, and it won't be serialized anyway.
                    };

                    // Replace the starting node with the coalesced node.
                    capturedItems[startInclusive] = coalesced;

                    // Now iterate through all the rest of the updates in the run, updating the coalesced node with relevant properties,
                    // and nulling out the nodes along the way. We do this rather than removing the entry in order to avoid an O(N^2) operation.
                    // We'll remove all the null entries at the end of the loop, using RemoveAll to do so, which can remove all of
                    // the nulls in a single O(N) pass.
                    for (int i = startInclusive + 1; i < endExclusive; i++)
                    {
                        // Grab the next item.
                        StreamingChatCompletionUpdate next = capturedItems[i];
                        capturedItems[i] = null!;

                        var nextContent = (TextContent)next.Contents[0];
                        _ = coalescedText.Append(nextContent.Text);

                        coalesced.AuthorName ??= next.AuthorName;
                        coalesced.CompletionId ??= next.CompletionId;
                        coalesced.CreatedAt ??= next.CreatedAt;
                        coalesced.FinishReason ??= next.FinishReason;
                        coalesced.ModelId ??= next.ModelId;
                        coalesced.Role ??= next.Role;
                    }

                    // Complete the coalescing by patching the text of the coalesced node.
                    coalesced.Text = coalescedText.ToString();

                    // Jump to the last update in the run, so that when we loop around and bump ahead,
                    // we're at the next update just after the run.
                    startInclusive = endExclusive - 1;
                }

                // Remove all of the null slots left over from the coalescing process.
                _ = capturedItems.RemoveAll(u => u is null);
            }

            // Write the captured items to the cache.
            await WriteCacheStreamingAsync(cacheKey, capturedItems, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Computes a cache key for the specified call parameters.
    /// </summary>
    /// <param name="streaming">A flag to indicate if this is a streaming call.</param>
    /// <param name="chatMessages">The chat content.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    protected abstract string GetCacheKey(bool streaming, IList<ChatMessage> chatMessages, ChatOptions? options);

    /// <summary>
    /// Returns a previously cached <see cref="ChatCompletion"/>, if available.
    /// This is used when there is a call to <see cref="IChatClient.CompleteAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<ChatCompletion?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a previously cached list of <see cref="StreamingChatCompletionUpdate"/> values, if available.
    /// This is used when there is a call to <see cref="IChatClient.CompleteStreamingAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<IReadOnlyList<StreamingChatCompletionUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a <see cref="ChatCompletion"/> in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.CompleteAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatCompletion"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheAsync(string key, ChatCompletion value, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a list of <see cref="StreamingChatCompletionUpdate"/> values in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.CompleteStreamingAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatCompletion"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheStreamingAsync(string key, IReadOnlyList<StreamingChatCompletionUpdate> value, CancellationToken cancellationToken);
}

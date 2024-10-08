// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

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
            foreach (var chunk in existingChunks)
            {
                yield return chunk;
            }
        }
        else
        {
            var capturedItems = new List<StreamingChatCompletionUpdate>();
            StreamingChatCompletionUpdate? previousCoalescedCopy = null;
            await foreach (var item in base.CompleteStreamingAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return item;

                // If this item is compatible with the previous one, we will coalesce them in the cache
                var previous = capturedItems.Count > 0 ? capturedItems[capturedItems.Count - 1] : null;
                if (item.ChoiceIndex == 0
                    && item.Contents.Count == 1
                    && item.Contents[0] is TextContent currentTextContent
                    && previous is { ChoiceIndex: 0 }
                    && previous.Role == item.Role
                    && previous.Contents is { Count: 1 }
                    && previous.Contents[0] is TextContent previousTextContent)
                {
                    if (!ReferenceEquals(previous, previousCoalescedCopy))
                    {
                        // We don't want to mutate any object that we also yield, since the recipient might
                        // not expect that. Instead make a copy we can safely mutate.
                        previousCoalescedCopy = new()
                        {
                            Role = previous.Role,
                            AuthorName = previous.AuthorName,
                            AdditionalProperties = previous.AdditionalProperties,
                            ChoiceIndex = previous.ChoiceIndex,
                            RawRepresentation = previous.RawRepresentation,
                            Contents = [new TextContent(previousTextContent.Text)]
                        };

                        // The last item we captured was before we knew it could be coalesced
                        // with this one, so replace it with the coalesced copy
                        capturedItems[capturedItems.Count - 1] = previousCoalescedCopy;
                    }

#pragma warning disable S1643 // Strings should not be concatenated using '+' in a loop
                    ((TextContent)previousCoalescedCopy.Contents[0]).Text += currentTextContent.Text;
#pragma warning restore S1643
                }
                else
                {
                    capturedItems.Add(item);
                }
            }

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

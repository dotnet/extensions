﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is not { } result)
        {
            result = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            await WriteCacheAsync(cacheKey, result, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        if (CoalesceStreamingUpdates)
        {
            // When coalescing updates, we cache non-streaming results coalesced from streaming ones. That means
            // we make a streaming request, yielding those results, but then convert those into a non-streaming
            // result and cache it. When we get a cache hit, we yield the non-streaming result as a streaming one.

            var cacheKey = GetCacheKey(true, chatMessages, options);
            if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is { } chatCompletion)
            {
                // Yield all of the cached items.
                foreach (var chunk in chatCompletion.ToStreamingChatCompletionUpdates())
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

                // Write the captured items to the cache as a non-streaming result.
                await WriteCacheAsync(cacheKey, capturedItems.ToChatCompletion(), cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
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

                // Write the captured items to the cache.
                await WriteCacheStreamingAsync(cacheKey, capturedItems, cancellationToken).ConfigureAwait(false);
            }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S127 // "for" loop stop conditions should be invariant

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a delegating chat client that caches the results of chat calls.
/// </summary>
public abstract class CachingChatClient : DelegatingChatClient
{
    /// <summary>A boxed <see langword="true"/> value.</summary>
    private static readonly object _boxedTrue = true;

    /// <summary>A boxed <see langword="false"/> value.</summary>
    private static readonly object _boxedFalse = false;

    /// <summary>Initializes a new instance of the <see cref="CachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    protected CachingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <summary>Gets or sets a value indicating whether streaming updates are coalesced.</summary>
    /// <value>
    /// <para>
    /// <see langword="true"/> if the client attempts to coalesce contiguous streaming updates
    /// into a single update, to reduce the number of individual items that are yielded on
    /// subsequent enumerations of the cached data; <see langword="false"/> if the updates are
    /// kept unaltered.
    /// </para>
    /// <para>
    /// The default is <see langword="true"/>.
    /// </para>
    /// </value>
    public bool CoalesceStreamingUpdates { get; set; } = true;

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
        // or having problems when one of the callers cancels but others don't. This has the drawback that
        // concurrent callers might trigger duplicate requests, but that's acceptable.
        var cacheKey = GetCacheKey(_boxedFalse, chatMessages, options);

        if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is not { } result)
        {
            result = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            await WriteCacheAsync(cacheKey, result, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        if (CoalesceStreamingUpdates)
        {
            // When coalescing updates, we cache non-streaming results coalesced from streaming ones. That means
            // we make a streaming request, yielding those results, but then convert those into a non-streaming
            // result and cache it. When we get a cache hit, we yield the non-streaming result as a streaming one.

            var cacheKey = GetCacheKey(_boxedTrue, chatMessages, options);
            if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is { } chatResponse)
            {
                // Yield all of the cached items.
                foreach (var chunk in chatResponse.ToChatResponseUpdates())
                {
                    yield return chunk;
                }
            }
            else
            {
                // Yield and store all of the items.
                List<ChatResponseUpdate> capturedItems = [];
                await foreach (var chunk in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache as a non-streaming result.
                await WriteCacheAsync(cacheKey, capturedItems.ToChatResponse(), cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var cacheKey = GetCacheKey(_boxedTrue, chatMessages, options);
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
                List<ChatResponseUpdate> capturedItems = [];
                await foreach (var chunk in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache.
                await WriteCacheStreamingAsync(cacheKey, capturedItems, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    protected abstract string GetCacheKey(params ReadOnlySpan<object?> values);

    /// <summary>
    /// Returns a previously cached <see cref="ChatResponse"/>, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a previously cached list of <see cref="ChatResponseUpdate"/> values, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a <see cref="ChatResponse"/> in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a list of <see cref="ChatResponseUpdate"/> values in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync(IList{ChatMessage}, ChatOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken);
}

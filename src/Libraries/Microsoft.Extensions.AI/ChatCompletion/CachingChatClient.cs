// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S127 // "for" loop stop conditions should be invariant
#pragma warning disable SA1202 // Elements should be ordered by access

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
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        return EnableCaching(messages, options) ?
            GetCachedResponseAsync(messages, options, cancellationToken) :
            base.GetResponseAsync(messages, options, cancellationToken);
    }

    private async Task<ChatResponse> GetCachedResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
        // or having problems when one of the callers cancels but others don't. This has the drawback that
        // concurrent callers might trigger duplicate requests, but that's acceptable.
        var cacheKey = GetCacheKey(messages, options, _boxedFalse);

        if (await ReadCacheAsync(cacheKey, cancellationToken) is not { } result)
        {
            result = await base.GetResponseAsync(messages, options, cancellationToken);
            await WriteCacheAsync(cacheKey, result, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        return EnableCaching(messages, options) ?
            GetCachedStreamingResponseAsync(messages, options, cancellationToken) :
            base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetCachedStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (CoalesceStreamingUpdates)
        {
            // When coalescing updates, we cache non-streaming results coalesced from streaming ones. That means
            // we make a streaming request, yielding those results, but then convert those into a non-streaming
            // result and cache it. When we get a cache hit, we yield the non-streaming result as a streaming one.

            var cacheKey = GetCacheKey(messages, options, _boxedTrue);
            if (await ReadCacheAsync(cacheKey, cancellationToken) is { } chatResponse)
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
                await foreach (var chunk in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache as a non-streaming result.
                await WriteCacheAsync(cacheKey, capturedItems.ToChatResponse(), cancellationToken);
            }
        }
        else
        {
            var cacheKey = GetCacheKey(messages, options, _boxedTrue);
            if (await ReadCacheStreamingAsync(cacheKey, cancellationToken) is { } existingChunks)
            {
                // Yield all of the cached items.
                string? conversationId = null;
                foreach (var chunk in existingChunks)
                {
                    conversationId ??= chunk.ConversationId;
                    yield return chunk;
                }
            }
            else
            {
                // Yield and store all of the items.
                List<ChatResponseUpdate> capturedItems = [];
                await foreach (var chunk in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache.
                await WriteCacheStreamingAsync(cacheKey, capturedItems, cancellationToken);
            }
        }
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="messages">The messages to inform the key.</param>
    /// <param name="options">The <see cref="ChatOptions"/> to inform the key.</param>
    /// <param name="additionalValues">Any other values to inform the key.</param>
    /// <returns>The computed key.</returns>
    protected abstract string GetCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options, params ReadOnlySpan<object?> additionalValues);

    /// <summary>
    /// Returns a previously cached <see cref="ChatResponse"/>, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    protected abstract Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a previously cached list of <see cref="ChatResponseUpdate"/> values, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    protected abstract Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a <see cref="ChatResponse"/> in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    protected abstract Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a list of <see cref="ChatResponseUpdate"/> values in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    protected abstract Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken);

    /// <summary>Determines whether caching should be used with the specified request.</summary>
    /// <param name="messages">The sequence of chat messages included in the request.</param>
    /// <param name="options">The chat options included in the request.</param>
    /// <returns>
    /// <see langword="true"/> if caching should be used for the request, such that the <see cref="CachingChatClient"/>
    /// will try to satisfy the request from the cache, or if it can't, will try to cache the fetched response.
    /// <see langword="false"/> if caching should not be used for the request, such that the request will
    /// be passed through to the inner <see cref="IChatClient"/> without attempting to read from or write to the cache.
    /// </returns>
    /// <remarks>
    /// The default implementation returns <see langword="true"/> as long as the <paramref name="options"/>
    /// does not have a <see cref="ChatOptions.ConversationId"/> set.
    /// </remarks>
    protected virtual bool EnableCaching(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        // We want to skip caching if options.ConversationId is set. If it's set, that implies there's
        // some state that will impact the response and that's not represented in the messages. Since
        // that state could change even with the same ID (e.g. if it's a thread ID representing the
        // mutable state of a conversation), we have to assume caching isn't valid.
        return options?.ConversationId is null;
    }
}

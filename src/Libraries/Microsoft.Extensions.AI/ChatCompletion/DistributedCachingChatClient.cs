// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that caches the results of completion calls, storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed
/// <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </remarks>
public class DistributedCachingChatClient : CachingChatClient
{
    /// <summary>A boxed <see langword="true"/> value.</summary>
    private static readonly object _boxedTrue = true;

    /// <summary>A boxed <see langword="false"/> value.</summary>
    private static readonly object _boxedFalse = false;

    /// <summary>The <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</summary>
    private readonly IDistributedCache _storage;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use when serializing cache data.</summary>
    private JsonSerializerOptions _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="storage">An <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    public DistributedCachingChatClient(IChatClient innerClient, IDistributedCache storage)
        : base(innerClient)
    {
        _storage = Throw.IfNull(storage);
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc />
    protected override async Task<ChatCompletion?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (ChatCompletion?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(ChatCompletion)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<StreamingChatCompletionUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (IReadOnlyList<StreamingChatCompletionUpdate>?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<StreamingChatCompletionUpdate>)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, ChatCompletion value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(ChatCompletion)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task WriteCacheStreamingAsync(string key, IReadOnlyList<StreamingChatCompletionUpdate> value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<StreamingChatCompletionUpdate>)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override string GetCacheKey(bool streaming, IList<ChatMessage> chatMessages, ChatOptions? options) =>
        GetCacheKey([streaming ? _boxedTrue : _boxedFalse, chatMessages, options]);

    /// <summary>Gets a cache key based on the supplied values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>This provides the default implementation for <see cref="GetCacheKey(bool, IList{ChatMessage}, ChatOptions?)"/>.</remarks>
    protected string GetCacheKey(ReadOnlySpan<object?> values)
    {
        _jsonSerializerOptions.MakeReadOnly();
        return CachingHelpers.GetCacheKey(values, _jsonSerializerOptions);
    }
}

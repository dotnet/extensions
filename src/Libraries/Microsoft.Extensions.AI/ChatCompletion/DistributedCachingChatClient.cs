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
/// A delegating chat client that caches the results of response calls, storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed
/// <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </remarks>
public class DistributedCachingChatClient : CachingChatClient
{
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
    protected override async Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (ChatResponse?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (IReadOnlyList<ChatResponseUpdate>?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>The <paramref name="values"/> are serialized to JSON using <see cref="JsonSerializerOptions"/> in order to compute the key.</remarks>
    protected override string GetCacheKey(params ReadOnlySpan<object?> values)
    {
        _jsonSerializerOptions.MakeReadOnly();
        return CachingHelpers.GetCacheKey(values, _jsonSerializerOptions);
    }
}

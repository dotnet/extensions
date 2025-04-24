// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1502 // Element should not be on a single line

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that caches the results of response calls, storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DistributedCachingChatClient"/> employs JSON serialization as part of storing cached data. It is not guaranteed that
/// the object models used by <see cref="ChatMessage"/>, <see cref="ChatOptions"/>, <see cref="ChatResponse"/>, <see cref="ChatResponseUpdate"/>,
/// or any of the other objects in the chat client pipeline will roundtrip through JSON serialization with full fidelity. For example,
/// <see cref="ChatMessage.RawRepresentation"/> will be ignored, and <see cref="object"/> values in <see cref="ChatMessage.AdditionalProperties"/>
/// will deserialize as <see cref="JsonElement"/> rather than as the original type. In general, code using <see cref="DistributedCachingChatClient"/>
/// should only rely on accessing data that can be preserved well enough through JSON serialization and deserialization.
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed
/// <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </para>
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

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
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

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
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
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="messages">The messages to inform the key.</param>
    /// <param name="options">The <see cref="ChatOptions"/> to inform the key.</param>
    /// <param name="additionalValues">Any other values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="messages"/>, <paramref name="options"/>, and <paramref name="additionalValues"/> are serialized to JSON using <see cref="JsonSerializerOptions"/>
    /// in order to compute the key.
    /// </para>
    /// <para>
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </para>
    /// </remarks>
    protected override string GetCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options, params ReadOnlySpan<object?> additionalValues)
    {
        // Bump the cache version to invalidate existing caches if the serialization format changes in a breaking way.
        const int CacheVersion = 1;

        return AIJsonUtilities.HashDataToString([CacheVersion, messages, options, .. additionalValues], _jsonSerializerOptions);
    }
}

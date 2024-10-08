// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
public class DistributedCachingChatClient : CachingChatClient
{
    private readonly IDistributedCache _storage;
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="storage">An <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    public DistributedCachingChatClient(IChatClient innerClient, IDistributedCache storage)
        : base(innerClient)
    {
        _storage = Throw.IfNull(storage);
        _jsonSerializerOptions = JsonDefaults.Options;
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
    protected override string GetCacheKey(bool streaming, IList<ChatMessage> chatMessages, ChatOptions? options)
    {
        // While it might be desirable to include ChatOptions in the cache key, it's not always possible,
        // since ChatOptions can contain types that are not guaranteed to be serializable or have a stable
        // hashcode across multiple calls. So the default cache key is simply the JSON representation of
        // the chat contents. Developers may subclass and override this to provide custom rules.
        _jsonSerializerOptions.MakeReadOnly();
        return CachingHelpers.GetCacheKey(chatMessages, streaming, _jsonSerializerOptions);
    }
}

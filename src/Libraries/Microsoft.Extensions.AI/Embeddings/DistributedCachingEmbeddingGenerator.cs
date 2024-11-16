// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a delegating embedding generator that caches the results of embedding generation calls,
/// storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
/// <remarks>
/// The provided implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> is thread-safe for concurrent
/// use so long as the employed <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </remarks>
public class DistributedCachingEmbeddingGenerator<TInput, TEmbedding> : CachingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly IDistributedCache _storage;
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="storage">A <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    public DistributedCachingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, IDistributedCache storage)
        : base(innerGenerator)
    {
        _ = Throw.IfNull(storage);
        _storage = storage;
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set
        {
            _ = Throw.IfNull(value);
            _jsonSerializerOptions = value;
        }
    }

    /// <inheritdoc />
    protected override async Task<TEmbedding?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return JsonSerializer.Deserialize(existingJson, (JsonTypeInfo<TEmbedding>)_jsonSerializerOptions.GetTypeInfo(typeof(TEmbedding)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, TEmbedding value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, (JsonTypeInfo<TEmbedding>)_jsonSerializerOptions.GetTypeInfo(typeof(TEmbedding)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override string GetCacheKey(params ReadOnlySpan<object?> values)
    {
        _jsonSerializerOptions.MakeReadOnly();
        return CachingHelpers.GetCacheKey(values, _jsonSerializerOptions);
    }
}

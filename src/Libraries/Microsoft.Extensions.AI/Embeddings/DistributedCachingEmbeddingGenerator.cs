// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    /// <summary>Boxed cache version.</summary>
    /// <remarks>Bump the cache version to invalidate existing caches if the serialization format changes in a breaking way.</remarks>
    private static readonly object _cacheVersion = 2;

    /// <summary>The <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</summary>
    private readonly IDistributedCache _storage;

    /// <summary>Additional values used to inform the cache key employed for storing state.</summary>
    private object[]? _cacheKeyAdditionalValues;

    /// <summary>Additional cache key values used to inform the key employed for storing state.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="storage">A <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> is <see langword="null"/>.</exception>
    public DistributedCachingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, IDistributedCache storage)
        : base(innerGenerator)
    {
        _ = Throw.IfNull(storage);
        _storage = storage;
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set
        {
            _ = Throw.IfNull(value);
            _jsonSerializerOptions = value;
        }
    }

    /// <summary>Gets or sets additional values used to inform the cache key employed for storing state.</summary>
    /// <remarks>Any values set in this list will augment the other values used to inform the cache key.</remarks>
    public IReadOnlyList<object>? CacheKeyAdditionalValues
    {
        get => _cacheKeyAdditionalValues;
        set => _cacheKeyAdditionalValues = value?.ToArray();
    }

    /// <inheritdoc />
    protected override async Task<TEmbedding?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
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
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="values"/> are serialized to JSON using <see cref="JsonSerializerOptions"/> in order to compute the key.
    /// </para>
    /// <para>
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </para>
    /// </remarks>
    [Experimental("MEAI001")]
    protected override string GetCacheKey(params ReadOnlySpan<object?> values)
    {
        const int FixedValuesCount = 1;

        object[] clientValues = _cacheKeyAdditionalValues ?? Array.Empty<object>();
        int length = FixedValuesCount + clientValues.Length + values.Length;

        object?[] arr = ArrayPool<object?>.Shared.Rent(length);
        try
        {
            arr[0] = _cacheVersion;
            values.CopyTo(arr.AsSpan(FixedValuesCount));
            clientValues.CopyTo(arr, FixedValuesCount + values.Length);

            return AIJsonUtilities.HashDataToString(arr.AsSpan(0, length), _jsonSerializerOptions);
        }
        finally
        {
            Array.Clear(arr, 0, length);
            ArrayPool<object?>.Shared.Return(arr);
        }
    }
}

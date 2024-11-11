// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating embedding generator that caches the results of embedding generation calls.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public abstract class CachingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>Initializes a new instance of the <see cref="CachingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    protected CachingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
        : base(innerGenerator)
    {
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        // Optimize for the common-case of a single value in a list/array.
        if (values is IList<TInput> valuesList)
        {
            switch (valuesList.Count)
            {
                case 0:
                    return [];

                case 1:
                    // In the expected common case where we can cheaply tell there's only a single value and access it,
                    // we can avoid all the overhead of splitting the list and reassembling it.
                    var cacheKey = GetCacheKey(valuesList[0], options);
                    if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is TEmbedding e)
                    {
                        return [e];
                    }
                    else
                    {
                        var generated = await base.GenerateAsync(valuesList, options, cancellationToken).ConfigureAwait(false);
                        if (generated.Count != 1)
                        {
                            throw new InvalidOperationException($"Expected exactly one embedding to be generated, but received {generated.Count}.");
                        }

                        await WriteCacheAsync(cacheKey, generated[0], cancellationToken).ConfigureAwait(false);
                        return generated;
                    }
            }
        }

        // Some of the inputs may already be cached. Go through each, checking to see whether each individually is cached.
        // Split those that are cached into one list and those that aren't into another. We retain their original positions
        // so that we can reassemble the results in the correct order.
        GeneratedEmbeddings<TEmbedding> results = [];
        List<(int Index, string CacheKey, TInput Input)>? uncached = null;
        foreach (TInput input in values)
        {
            // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
            // or having problems when one of the callers cancels but others don't. This has the drawback that
            // concurrent callers might trigger duplicate requests, but that's acceptable.
            var cacheKey = GetCacheKey(input, options);

            if (await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is TEmbedding existing)
            {
                results.Add(existing);
            }
            else
            {
                (uncached ??= []).Add((results.Count, cacheKey, input));
                results.Add(null!); // temporary placeholder
            }
        }

        // If anything wasn't cached, we need to generate embeddings for those.
        if (uncached is not null)
        {
            // Now make a single call to the wrapped generator to generate embeddings for all of the uncached inputs.
            var uncachedResults = await base.GenerateAsync(uncached.Select(e => e.Input), options, cancellationToken).ConfigureAwait(false);

            // Store the resulting embeddings into the cache individually.
            for (int i = 0; i < uncachedResults.Count; i++)
            {
                await WriteCacheAsync(uncached[i].CacheKey, uncachedResults[i], cancellationToken).ConfigureAwait(false);
            }

            // Fill in the gaps with the newly generated results.
            for (int i = 0; i < uncachedResults.Count; i++)
            {
                results[uncached[i].Index] = uncachedResults[i];
            }
        }

        Debug.Assert(results.All(e => e is not null), "Expected all values to be non-null");
        return results;
    }

    /// <summary>
    /// Computes a cache key for the specified call parameters.
    /// </summary>
    /// <param name="value">The <typeparamref name="TInput"/> for which an embedding is being requested.</param>
    /// <param name="options">The options to configure the request.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    protected abstract string GetCacheKey(TInput value, EmbeddingGenerationOptions? options);

    /// <summary>Returns a previously cached <see cref="Embedding{TEmbedding}"/>, if available.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<TEmbedding?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores a <typeparamref name="TEmbedding"/> in the underlying cache.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <typeparamref name="TEmbedding"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheAsync(string key, TEmbedding value, CancellationToken cancellationToken);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for adding a <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> to an
/// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> pipeline.
/// </summary>
public static class DistributedCachingEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> as the next stage in the pipeline.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> provided as <paramref name="builder"/>.</returns>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseDistributedCache<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        IDistributedCache? storage = null,
        Action<DistributedCachingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);
        return builder.Use((innerGenerator, services) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var result = new DistributedCachingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, storage);
            configure?.Invoke(result);
            return result;
        });
    }
}

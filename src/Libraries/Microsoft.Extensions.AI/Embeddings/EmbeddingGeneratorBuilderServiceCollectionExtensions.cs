// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for registering <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> with a <see cref="IServiceCollection"/>.</summary>
public static class EmbeddingGeneratorBuilderServiceCollectionExtensions
{
    /// <summary>Adds a embedding generator to the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="generatorFactory">The factory to use to construct the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="services"/> collection.</returns>
    /// <remarks>The generator is registered as a scoped service.</remarks>
    public static IServiceCollection AddEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection services,
        Func<EmbeddingGeneratorBuilder<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(generatorFactory);

        return services.AddScoped(services =>
            generatorFactory(new EmbeddingGeneratorBuilder<TInput, TEmbedding>(services)));
    }

    /// <summary>Adds an embedding generator to the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the service should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="generatorFactory">The factory to use to construct the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="services"/> collection.</returns>
    /// <remarks>The generator is registered as a scoped service.</remarks>
    public static IServiceCollection AddKeyedEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection services,
        object serviceKey,
        Func<EmbeddingGeneratorBuilder<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(generatorFactory);

        return services.AddKeyedScoped(serviceKey, (services, _) =>
            generatorFactory(new EmbeddingGeneratorBuilder<TInput, TEmbedding>(services)));
    }
}

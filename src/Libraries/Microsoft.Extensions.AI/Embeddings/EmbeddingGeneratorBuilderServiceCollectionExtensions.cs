// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> with a <see cref="IServiceCollection"/>.</summary>
public static class EmbeddingGeneratorBuilderServiceCollectionExtensions
{
    /// <summary>Registers an embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <typeparam name="TEmbeddingGenerator">The type of the embedding generator to register.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddEmbeddingGenerator<TInput, TEmbedding, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEmbeddingGenerator>(
        this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
        where TEmbeddingGenerator : IEmbeddingGenerator<TInput, TEmbedding>
        => AddEmbeddingGenerator(serviceCollection, provider => ActivatorUtilities.CreateInstance<TEmbeddingGenerator>(provider), lifetime);

    /// <summary>Registers an embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGenerator">The inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
        => AddEmbeddingGenerator(serviceCollection, _ => innerGenerator, lifetime);

    /// <summary>Registers an embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator<TInput, TEmbedding>), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers an embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <typeparam name="TEmbeddingGenerator">The type of the embedding generator to register.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddKeyedEmbeddingGenerator<TInput, TEmbedding, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEmbeddingGenerator>(
        this IServiceCollection serviceCollection,
        object serviceKey,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
        where TEmbeddingGenerator : IEmbeddingGenerator<TInput, TEmbedding>
        => AddKeyedEmbeddingGenerator(serviceCollection, serviceKey, provider => ActivatorUtilities.CreateInstance<TEmbeddingGenerator>(provider), lifetime);

    /// <summary>Registers a keyed embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="innerGenerator">The inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddKeyedEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        object serviceKey,
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
        => AddKeyedEmbeddingGenerator(serviceCollection, serviceKey, _ => innerGenerator, lifetime);

    /// <summary>Registers a keyed embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddKeyedEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        object serviceKey,
        Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator<TInput, TEmbedding>), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

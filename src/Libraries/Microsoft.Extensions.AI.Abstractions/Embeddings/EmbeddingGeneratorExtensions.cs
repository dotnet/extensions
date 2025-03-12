﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S2302 // "nameof" should be used
#pragma warning disable S4136 // Method overloads should be grouped together

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> instances.</summary>
public static class EmbeddingGeneratorExtensions
{
    /// <summary>Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(
        this IEmbeddingGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        return generator.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>
    /// Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of the specified type <paramref name="serviceType"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of services that are required to be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static object GetRequiredService(
        this IEmbeddingGenerator generator, Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(serviceType);

        return
            generator.GetService(serviceType, serviceKey) ??
            throw Throw.CreateMissingServiceException(serviceType, serviceKey);
    }

    /// <summary>
    /// Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of type <typeparamref name="TService"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that are required to be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static TService GetRequiredService<TService>(
        this IEmbeddingGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        if (generator.GetService(typeof(TService), serviceKey) is not TService service)
        {
            throw Throw.CreateMissingServiceException(typeof(TService), serviceKey);
        }

        return service;
    }

    /// <summary>Generates an embedding vector from the specified <paramref name="value"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbeddingElement">The numeric type of the embedding data.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="value">A value from which an embedding will be generated.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embedding for the specified <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce exactly one embedding.</exception>
    /// <remarks>
    /// This operation is equivalent to using <see cref="GenerateEmbeddingAsync"/> and returning the
    /// resulting <see cref="Embedding{T}"/>'s <see cref="Embedding{T}.Vector"/> property.
    /// </remarks>
    public static async Task<ReadOnlyMemory<TEmbeddingElement>> GenerateEmbeddingVectorAsync<TInput, TEmbeddingElement>(
        this IEmbeddingGenerator<TInput, Embedding<TEmbeddingElement>> generator,
        TInput value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GenerateEmbeddingAsync(generator, value, options, cancellationToken).ConfigureAwait(false);
        return embedding.Vector;
    }

    /// <summary>Generates an embedding from the specified <paramref name="value"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding to generate.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="value">A value from which an embedding will be generated.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// The generated embedding for the specified <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce exactly one embedding.</exception>
    /// <remarks>
    /// This operations is equivalent to using <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/> with a
    /// collection composed of the single <paramref name="value"/> and then returning the first embedding element from the
    /// resulting <see cref="GeneratedEmbeddings{TEmbedding}"/> collection.
    /// </remarks>
    public static async Task<TEmbedding> GenerateEmbeddingAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> generator,
        TInput value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(value);

        var embeddings = await generator.GenerateAsync([value], options, cancellationToken).ConfigureAwait(false);

        if (embeddings is null)
        {
            Throw.InvalidOperationException("Embedding generator returned a null collection of embeddings.");
        }

        if (embeddings.Count != 1)
        {
            Throw.InvalidOperationException($"Expected the number of embeddings ({embeddings.Count}) to match the number of inputs (1).");
        }

        TEmbedding embedding = embeddings[0];
        if (embedding is null)
        {
            Throw.InvalidOperationException("Embedding generator generated a null embedding.");
        }

        return embedding;
    }

    /// <summary>
    /// Generates embeddings for each of the supplied <paramref name="values"/> and produces a list that pairs
    /// each input value with its resulting embedding.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding to generate.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="values">The collection of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An array containing tuples of the input values and the associated generated embeddings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce one embedding for each input value.</exception>
    public static async Task<(TInput Value, TEmbedding Embedding)[]> GenerateAndZipAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> generator,
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(values);

        IList<TInput> inputs = values as IList<TInput> ?? values.ToList();
        int inputsCount = inputs.Count;

        if (inputsCount == 0)
        {
            return Array.Empty<(TInput, TEmbedding)>();
        }

        var embeddings = await generator.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        if (embeddings.Count != inputsCount)
        {
            Throw.InvalidOperationException($"Expected the number of embeddings ({embeddings.Count}) to match the number of inputs ({inputsCount}).");
        }

        var results = new (TInput, TEmbedding)[embeddings.Count];
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = (inputs[i], embeddings[i]);
        }

        return results;
    }
}

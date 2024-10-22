// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> instances.</summary>
public static class EmbeddingGeneratorExtensions
{
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
        if (embeddings.Count != 1)
        {
            throw new InvalidOperationException("Expected exactly one embedding to be generated.");
        }

        return embeddings[0];
    }

    /// <summary>Generates an embedding vector from the specified <paramref name="value"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The numeric type of the embedding data.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="value">A value from which an embedding will be generated.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embedding for the specified <paramref name="value"/>.</returns>
    /// <remarks>
    /// This operation is equivalent to using <see cref="GenerateEmbeddingAsync"/> and returning the
    /// resulting <see cref="Embedding{T}"/>'s <see cref="Embedding{T}.Vector"/> property.
    /// </remarks>
    public static async Task<ReadOnlyMemory<TEmbedding>> GenerateEmbeddingVectorAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, Embedding<TEmbedding>> generator,
        TInput value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GenerateEmbeddingAsync(generator, value, options, cancellationToken).ConfigureAwait(false);
        return embedding.Vector;
    }

    /// <summary>
    /// Generates embeddings for each of the supplied <paramref name="values"/> and produces a list that pairs
    /// each input with its resulting embedding.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding to generate.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="values">The collection of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embeddings.</returns>
    public static async Task<IList<KeyValuePair<TInput, TEmbedding>>> GenerateAndZipEmbeddingsAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> generator,
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(values);

        IList<TInput> inputs = values as IList<TInput> ?? values.ToList();

        var embeddings = await generator.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        if (embeddings.Count != inputs.Count)
        {
            throw new InvalidOperationException($"Expected the number of embeddings ({embeddings.Count}) to match the number of inputs ({inputs.Count}).");
        }

        List<KeyValuePair<TInput, TEmbedding>> results = new(embeddings.Count);
        for (int i = 0; i < embeddings.Count; i++)
        {
            results.Add(new KeyValuePair<TInput, TEmbedding>(inputs[i], embeddings[i]));
        }

        return results;
    }
}

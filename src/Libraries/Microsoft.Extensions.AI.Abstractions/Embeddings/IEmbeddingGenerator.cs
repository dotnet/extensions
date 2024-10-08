// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a generator of embeddings.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public interface IEmbeddingGenerator<TInput, TEmbedding> : IDisposable
    where TEmbedding : Embedding
{
    /// <summary>Generates embeddings for each of the supplied <paramref name="values"/>.</summary>
    /// <param name="values">The collection of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embeddings.</returns>
    Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets metadata that describes the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
    EmbeddingGeneratorMetadata Metadata { get; }

    /// <summary>Asks the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="key">An optional key that may be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly-typed services that may be provided by the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    TService? GetService<TService>(object? key = null)
        where TService : class;
}

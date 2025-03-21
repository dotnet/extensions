// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a generator of embeddings.</summary>
/// <remarks>
/// This base interface is used to allow for embedding generators to be stored in a non-generic manner.
/// To use the generator to create embeddings, instances typed as this base interface first need to be
/// cast to the generic interface <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
/// </remarks>
public interface IEmbeddingGenerator : IDisposable
{
    /// <summary>Asks the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the
    /// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>, including itself or any services it might be wrapping.
    /// For example, to access the <see cref="EmbeddingGeneratorMetadata"/> for the instance, <see cref="GetService"/> may
    /// be used to request it.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}

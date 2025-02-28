// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>
/// in the context of <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</summary>
public static class EmbeddingGeneratorBuilderEmbeddingGeneratorExtensions
{
    /// <summary>
    /// Creates a new <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> using
    /// <paramref name="innerGenerator"/> as its inner generator.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="innerGenerator">The generator to use as the inner generator.</param>
    /// <returns>The new <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>
    /// constructor directly, specifying <paramref name="innerGenerator"/> as the inner generator.
    /// </remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AsBuilder<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(innerGenerator);

        return new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGenerator);
    }
}

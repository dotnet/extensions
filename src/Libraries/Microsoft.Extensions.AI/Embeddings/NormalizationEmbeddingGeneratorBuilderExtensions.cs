// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Numerics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="NormalizationEmbeddingGenerator{TInput, TEmbeddingValue}"/> instances.</summary>
public static class NormalizationEmbeddingGeneratorBuilderExtensions
{
    /// <summary>Adds embedding vector normalization to the embedding generator pipeline.</summary>
    /// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbeddingValue">Specifies the type of the values in the embeddings produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, Embedding}"/>.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="NormalizationEmbeddingGenerator{TInput, TEmbeddingValue}"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// Most embedding generators produce normalized vectors, so this operation typically is not needed. However, if a generator
    /// is known to produce non-normalized vectors, this operation may be used to normalize those vectors. The implementation performs
    /// an operation equivalent to <c>newVector[i] = oldVector[i] / EuclideanNorm(oldVector)</c>.
    /// </remarks>
    public static EmbeddingGeneratorBuilder<TInput, Embedding<TEmbeddingValue>> UseNormalization<TInput, TEmbeddingValue>(
        this EmbeddingGeneratorBuilder<TInput, Embedding<TEmbeddingValue>> builder, Action<NormalizationEmbeddingGenerator<TInput, TEmbeddingValue>>? configure = null)
        where TEmbeddingValue : IRootFunctions<TEmbeddingValue>
    {
        _ = Throw.IfNull(builder);

        return builder.Use((services, innerGenerator) =>
        {
            var generator = new NormalizationEmbeddingGenerator<TInput, TEmbeddingValue>(innerGenerator);
            configure?.Invoke(generator);
            return generator;
        });
    }
}
#endif

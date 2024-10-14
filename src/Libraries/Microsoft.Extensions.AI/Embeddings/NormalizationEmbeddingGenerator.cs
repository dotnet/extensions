// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET // requires generic math interfaces
using System.Collections.Generic;
using System.Numerics;
using System.Numerics.Tensors;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that normalizes embedding vectors not already annotated as being normalized.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbeddingValue">Specifies the type of the values in the embedding vectors produced by the generator.</typeparam>
public sealed class NormalizationEmbeddingGenerator<TInput, TEmbeddingValue> : DelegatingEmbeddingGenerator<TInput, Embedding<TEmbeddingValue>>
    where TEmbeddingValue : IRootFunctions<TEmbeddingValue>
{
    /// <summary>Initializes a new instance of the <see cref="NormalizationEmbeddingGenerator{TInput, TEmbeddingElement}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, Embedding}"/>.</param>
    public NormalizationEmbeddingGenerator(IEmbeddingGenerator<TInput, Embedding<TEmbeddingValue>> innerGenerator)
        : base(innerGenerator)
    {
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<Embedding<TEmbeddingValue>>> GenerateAsync(
        IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var embeddings = await InnerGenerator.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);

        foreach (var e in embeddings)
        {
            if (!e.Normalized)
            {
                var normalized = new TEmbeddingValue[e.Vector.Length];
                TensorPrimitives.Divide(e.Vector.Span, TensorPrimitives.Norm(e.Vector.Span), normalized);
                e.Vector = normalized;
                e.Normalized = true;
            }
        }

        return embeddings;
    }
}
#endif

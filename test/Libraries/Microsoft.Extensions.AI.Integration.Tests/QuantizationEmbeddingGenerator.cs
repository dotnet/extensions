// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
#if NET
using System.Numerics.Tensors;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal sealed class QuantizationEmbeddingGenerator :
    IEmbeddingGenerator<string, BinaryEmbedding>
#if NET
    , IEmbeddingGenerator<string, Embedding<Half>>
#endif
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _floatService;

    public QuantizationEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> floatService)
    {
        _floatService = floatService;
    }

    void IDisposable.Dispose() => _floatService.Dispose();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
        _floatService.GetService(serviceType, serviceKey);

    async Task<GeneratedEmbeddings<BinaryEmbedding>> IEmbeddingGenerator<string, BinaryEmbedding>.GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options, CancellationToken cancellationToken)
    {
        var embeddings = await _floatService.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        return new(from e in embeddings select QuantizeToBinary(e))
        {
            Usage = embeddings.Usage,
            AdditionalProperties = embeddings.AdditionalProperties,
        };
    }

    private static BinaryEmbedding QuantizeToBinary(Embedding<float> embedding)
    {
        ReadOnlySpan<float> vector = embedding.Vector.Span;

        var result = new byte[(int)Math.Ceiling(vector.Length / 8.0)];
        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] > 0)
            {
                result[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return new(result)
        {
            CreatedAt = embedding.CreatedAt,
            ModelId = embedding.ModelId,
            AdditionalProperties = embedding.AdditionalProperties,
        };
    }

#if NET
    async Task<GeneratedEmbeddings<Embedding<Half>>> IEmbeddingGenerator<string, Embedding<Half>>.GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options, CancellationToken cancellationToken)
    {
        var embeddings = await _floatService.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        return new(from e in embeddings select QuantizeToHalf(e))
        {
            Usage = embeddings.Usage,
            AdditionalProperties = embeddings.AdditionalProperties,
        };
    }

    private static Embedding<Half> QuantizeToHalf(Embedding<float> embedding)
    {
        ReadOnlySpan<float> vector = embedding.Vector.Span;
        var result = new Half[vector.Length];
        TensorPrimitives.ConvertToHalf(vector, result);
        return new(result)
        {
            CreatedAt = embedding.CreatedAt,
            ModelId = embedding.ModelId,
            AdditionalProperties = embedding.AdditionalProperties,
        };
    }
#endif
}

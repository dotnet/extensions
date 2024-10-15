// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class NormalizationEmbeddingGeneratorTests
{
    [Fact]
    public void NormalizationEmbeddingGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new NormalizationEmbeddingGenerator<string, float>(null!));
        Assert.Throws<ArgumentNullException>("builder", () => NormalizationEmbeddingGeneratorBuilderExtensions.UseNormalization<string, float>(null!));
    }

    [Fact]
    public async Task NormalizedVectorsNotAttributedAsNormalized()
    {
        var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
        var vector2 = new float[] { 4.0f, 5.0f, 6.0f };

        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(new[]
                {
                    new Embedding<float>(vector1),
                    new Embedding<float>(vector2),
                    new Embedding<float>(Array.Empty<float>()),
                }));
            },
        };

        using var generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>()
            .UseNormalization()
            .Use(innerGenerator);

        var embeddings = await generator.GenerateAsync(["input1", "input2"]);

        Assert.Equal(3, embeddings.Count);

        Assert.True(MemoryMarshal.TryGetArray(embeddings[0].Vector, out ArraySegment<float> array1));
        Assert.True(MemoryMarshal.TryGetArray(embeddings[1].Vector, out ArraySegment<float> array2));
        Assert.True(MemoryMarshal.TryGetArray(embeddings[2].Vector, out ArraySegment<float> array3));

        Assert.NotSame(vector1, array1.Array);
        Assert.NotSame(vector2, array2.Array);
        Assert.NotSame(Array.Empty<float>(), array3.Array);

        Assert.Equal(3, array1.Array!.Length);
        Assert.Equal(3, array2.Array!.Length);
        Assert.Empty(array3.Array!);

        Assert.Equal(1.0f / 3.74165f, array1.Array[0], tolerance: 0.00001);
        Assert.Equal(2.0f / 3.74165f, array1.Array[1], tolerance: 0.00001);
        Assert.Equal(3.0f / 3.74165f, array1.Array[2], tolerance: 0.00001);

        Assert.Equal(4.0f / 8.77496f, array2.Array[0], tolerance: 0.00001);
        Assert.Equal(5.0f / 8.77496f, array2.Array[1], tolerance: 0.00001);
        Assert.Equal(6.0f / 8.77496f, array2.Array[2], tolerance: 0.00001);
    }
}
#endif

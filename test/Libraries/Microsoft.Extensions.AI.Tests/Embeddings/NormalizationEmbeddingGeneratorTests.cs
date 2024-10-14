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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NormalizedVectorsNotAttributedAsNormalized(bool normalized)
    {
        var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
        var vector2 = new float[] { 4.0f, 5.0f, 6.0f };

        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(new[]
                {
                    new Embedding<float>(vector1) { Normalized = normalized },
                    new Embedding<float>(vector2) { Normalized = normalized },
                    new Embedding<float>(Array.Empty<float>()) { Normalized = normalized },
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

        if (normalized)
        {
            Assert.Same(vector1, array1.Array);
            Assert.Equal(new[] { 1.0f, 2.0f, 3.0f }, array1.Array);

            Assert.Same(vector2, array2.Array);
            Assert.Equal(new[] { 4.0f, 5.0f, 6.0f }, array2.Array);

            Assert.Same(Array.Empty<float>(), array3.Array);
        }
        else
        {
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
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorExtensionsTests
{
    [Fact]
    public async Task GenerateAsync_InvalidArgs_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () => ((TestEmbeddingGenerator)null!).GenerateEmbeddingAsync("hello"));
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () => ((TestEmbeddingGenerator)null!).GenerateEmbeddingVectorAsync("hello"));
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () => ((TestEmbeddingGenerator)null!).GenerateAndZipAsync(["hello"]));
    }

    [Fact]
    public async Task GenerateAsync_ReturnsSingleEmbeddingAsync()
    {
        Embedding<float> result = new(new float[] { 1f, 2f, 3f });

        using TestEmbeddingGenerator service = new()
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
                Task.FromResult<GeneratedEmbeddings<Embedding<float>>>([result])
        };

        Assert.Same(result, await service.GenerateEmbeddingAsync("hello"));
        Assert.Equal(result.Vector, await service.GenerateEmbeddingVectorAsync("hello"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public async Task GenerateAndZipEmbeddingsAsync_ReturnsExpectedList(int count)
    {
        string[] inputs = Enumerable.Range(0, count).Select(i => $"hello {i}").ToArray();
        Embedding<float>[] embeddings = Enumerable
            .Range(0, count)
            .Select(i => new Embedding<float>(Enumerable.Range(i, 4).Select(i => (float)i).ToArray()))
            .ToArray();

        using TestEmbeddingGenerator service = new()
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
                Task.FromResult<GeneratedEmbeddings<Embedding<float>>>(new(embeddings))
        };

        var results = await service.GenerateAndZipAsync(inputs);
        Assert.NotNull(results);
        Assert.Equal(count, results.Length);
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(inputs[i], results[i].Value);
            Assert.Same(embeddings[i], results[i].Embedding);
        }
    }
}

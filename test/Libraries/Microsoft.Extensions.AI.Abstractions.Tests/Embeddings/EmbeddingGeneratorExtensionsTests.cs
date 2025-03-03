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
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("generator", () => EmbeddingGeneratorExtensions.GetService<object>(null!));
        Assert.Throws<ArgumentNullException>("generator", () => EmbeddingGeneratorExtensions.GetService<string, Embedding<double>, object>(null!));
    }

    [Fact]
    public void GetRequiredService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("generator", () => EmbeddingGeneratorExtensions.GetRequiredService<object>(null!));
        Assert.Throws<ArgumentNullException>("generator", () => EmbeddingGeneratorExtensions.GetRequiredService<string, Embedding<double>>(null!, typeof(string)));
        Assert.Throws<ArgumentNullException>("generator", () => EmbeddingGeneratorExtensions.GetRequiredService<string, Embedding<double>, object>(null!));

        using var generator = new TestEmbeddingGenerator();
        Assert.Throws<ArgumentNullException>("serviceType", () => generator.GetRequiredService(null!));
    }

    [Fact]
    public void GetService_ValidService_Returned()
    {
        using IEmbeddingGenerator<string, Embedding<float>> generator = new TestEmbeddingGenerator
        {
            GetServiceCallback = (Type serviceType, object? serviceKey) =>
            {
                if (serviceType == typeof(string))
                {
                    return serviceKey == null ? "null key" : "non-null key";
                }

                if (serviceType == typeof(IEmbeddingGenerator<string, Embedding<float>>))
                {
                    return new object();
                }

                return null;
            },
        };

        Assert.Equal("null key", generator.GetService(typeof(string)));
        Assert.Equal("null key", generator.GetService<string>());
        Assert.Equal("null key", generator.GetService<string, Embedding<float>, string>());

        Assert.Equal("non-null key", generator.GetService(typeof(string), "key"));
        Assert.Equal("non-null key", generator.GetService<string>("key"));
        Assert.Equal("non-null key", generator.GetService<string, Embedding<float>, string>("key"));

        Assert.Null(generator.GetService(typeof(object)));
        Assert.Null(generator.GetService<object>());
        Assert.Null(generator.GetService<string, Embedding<float>, object>());

        Assert.Null(generator.GetService(typeof(object), "key"));
        Assert.Null(generator.GetService<object>("key"));
        Assert.Null(generator.GetService<string, Embedding<float>, object>("key"));

        Assert.Null(generator.GetService<int?>());
        Assert.Null(generator.GetService<string, Embedding<float>, int?>());

        Assert.Equal("null key", generator.GetRequiredService(typeof(string)));
        Assert.Equal("null key", generator.GetRequiredService<string>());
        Assert.Equal("null key", generator.GetRequiredService<string, Embedding<float>, string>());

        Assert.Equal("non-null key", generator.GetRequiredService(typeof(string), "key"));
        Assert.Equal("non-null key", generator.GetRequiredService<string>("key"));
        Assert.Equal("non-null key", generator.GetRequiredService<string, Embedding<float>, string>("key"));

        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService(typeof(object)));
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<object>());
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<string, Embedding<float>, object>());

        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService(typeof(object), "key"));
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<object>("key"));
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<string, Embedding<float>, object>("key"));

        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<int?>());
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<string, Embedding<float>, int?>());
    }

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorBuilderTests
{
    [Fact]
    public void PassesServiceProviderToFactories()
    {
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using var expectedResult = new TestEmbeddingGenerator();
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>(expectedServiceProvider);

        builder.Use((serviceProvider, innerClient) =>
        {
            Assert.Same(expectedServiceProvider, serviceProvider);
            return expectedResult;
        });

        using var innerGenerator = new TestEmbeddingGenerator();
        Assert.Equal(expectedResult, builder.Use(innerGenerator));
    }

    [Fact]
    public void BuildsPipelineInOrderAdded()
    {
        // Arrange
        using var expectedInnerService = new TestEmbeddingGenerator();
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>();

        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("First", next));
        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("Second", next));
        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("Third", next));

        // Act
        var first = (InnerServiceCapturingEmbeddingGenerator)builder.Use(expectedInnerService);

        // Assert
        Assert.Equal("First", first.Name);
        var second = (InnerServiceCapturingEmbeddingGenerator)first.InnerGenerator;
        Assert.Equal("Second", second.Name);
        var third = (InnerServiceCapturingEmbeddingGenerator)second.InnerGenerator;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerService, third.InnerGenerator);
    }

    [Fact]
    public void DoesNotAcceptNullInnerService()
    {
        Assert.Throws<ArgumentNullException>(() => new EmbeddingGeneratorBuilder<string, Embedding<float>>().Use((IEmbeddingGenerator<string, Embedding<float>>)null!));
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>();
        Assert.Throws<ArgumentNullException>(() => builder.Use((Func<IEmbeddingGenerator<string, Embedding<float>>, IEmbeddingGenerator<string, Embedding<float>>>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.Use((Func<IServiceProvider, IEmbeddingGenerator<string, Embedding<float>>, IEmbeddingGenerator<string, Embedding<float>>>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>();
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Use(new TestEmbeddingGenerator()));
        Assert.Contains("entry at index 0", ex.Message);
    }

    private sealed class InnerServiceCapturingEmbeddingGenerator(string name, IEmbeddingGenerator<string, Embedding<float>> innerGenerator) :
        DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
    {
#pragma warning disable S3604 // False positive: Member initializer values should not be redundant
        public string Name { get; } = name;
#pragma warning restore S3604
        public new IEmbeddingGenerator<string, Embedding<float>> InnerGenerator => base.InnerGenerator;
    }
}

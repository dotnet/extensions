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
        using var expectedOuterGenerator = new TestEmbeddingGenerator();
        using var expectedInnerGenerator = new TestEmbeddingGenerator();

        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>(services =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedInnerGenerator;
        });

        builder.Use((innerClient, services) =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedOuterGenerator;
        });

        Assert.Equal(expectedOuterGenerator, builder.Build(expectedServiceProvider));
    }

    [Fact]
    public void BuildsPipelineInOrderAdded()
    {
        // Arrange
        using var expectedInnerGenerator = new TestEmbeddingGenerator();
        var builder = expectedInnerGenerator.AsBuilder();

        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("First", next));
        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("Second", next));
        builder.Use(next => new InnerServiceCapturingEmbeddingGenerator("Third", next));

        // Act
        var first = (InnerServiceCapturingEmbeddingGenerator)builder.Build();

        // Assert
        Assert.Equal("First", first.Name);
        var second = (InnerServiceCapturingEmbeddingGenerator)first.InnerGenerator;
        Assert.Equal("Second", second.Name);
        var third = (InnerServiceCapturingEmbeddingGenerator)second.InnerGenerator;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerGenerator, third.InnerGenerator);
    }

    [Fact]
    public void DoesNotAcceptNullInnerService()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new EmbeddingGeneratorBuilder<string, Embedding<float>>((IEmbeddingGenerator<string, Embedding<float>>)null!));
        Assert.Throws<ArgumentNullException>("innerGenerator", () => ((IEmbeddingGenerator<string, Embedding<float>>)null!).AsBuilder());
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        Assert.Throws<ArgumentNullException>("innerGeneratorFactory",
            () => new EmbeddingGeneratorBuilder<string, Embedding<float>>((Func<IServiceProvider, IEmbeddingGenerator<string, Embedding<float>>>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        using var innerGenerator = new TestEmbeddingGenerator();
        var builder = innerGenerator.AsBuilder();
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
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

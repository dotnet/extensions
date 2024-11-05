// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsEmbeddingGeneratorTests
{
    [Fact]
    public void ConfigureOptionsEmbeddingGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new ConfigureOptionsEmbeddingGenerator<string, Embedding<float>>(null!, _ => new EmbeddingGenerationOptions()));
        Assert.Throws<ArgumentNullException>("configureOptions", () => new ConfigureOptionsEmbeddingGenerator<string, Embedding<float>>(new TestEmbeddingGenerator(), null!));
    }

    [Fact]
    public void UseEmbeddingGenerationOptions_InvalidArgs_Throws()
    {
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>();
        Assert.Throws<ArgumentNullException>("configureOptions", () => builder.UseEmbeddingGenerationOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullReturned)
    {
        EmbeddingGenerationOptions providedOptions = new();
        EmbeddingGenerationOptions? returnedOptions = nullReturned ? null : new();
        GeneratedEmbeddings<Embedding<float>> expectedEmbeddings = [];
        using CancellationTokenSource cts = new();

        using IEmbeddingGenerator<string, Embedding<float>> innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (inputs, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedEmbeddings);
            }
        };

        using var generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>()
            .UseEmbeddingGenerationOptions(options =>
            {
                Assert.Same(providedOptions, options);
                return returnedOptions;
            })
            .Use(innerGenerator);

        var embeddings = await generator.GenerateAsync([], providedOptions, cts.Token);
        Assert.Same(expectedEmbeddings, embeddings);
    }
}

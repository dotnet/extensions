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
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new ConfigureOptionsEmbeddingGenerator<string, Embedding<float>>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsEmbeddingGenerator<string, Embedding<float>>(new TestEmbeddingGenerator(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerGenerator = new TestEmbeddingGenerator();
        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>(innerGenerator);
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        EmbeddingGenerationOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        EmbeddingGenerationOptions? returnedOptions = null;
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

        using var generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(innerGenerator)
            .ConfigureOptions(options =>
            {
                Assert.NotSame(providedOptions, options);
                if (nullProvidedOptions)
                {
                    Assert.Null(options.ModelId);
                }
                else
                {
                    Assert.Equal(providedOptions!.ModelId, options.ModelId);
                }

                returnedOptions = options;
            })
            .Build();

        var embeddings = await generator.GenerateAsync([], providedOptions, cts.Token);
        Assert.Same(expectedEmbeddings, embeddings);
    }
}

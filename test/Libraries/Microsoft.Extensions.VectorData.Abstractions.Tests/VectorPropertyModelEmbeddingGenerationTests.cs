// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ProviderServices;
using Xunit;

namespace VectorData.UnitTests;

#pragma warning disable CA2000 // Dispose objects before losing scope

public class VectorPropertyModelEmbeddingGenerationTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_passes_Dimensions_to_generator()
    {
        var generator = new RecordingEmbeddingGenerator<string>();
        var property = CreateProperty(new VectorPropertyModel("Embedding", typeof(string)), generator, dimensions: 256);

        await property.GenerateEmbeddingAsync("text", CancellationToken.None);

        Assert.Equal(256, Assert.Single(generator.ReceivedOptions)?.Dimensions);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_passes_Dimensions_to_generator()
    {
        var generator = new RecordingEmbeddingGenerator<string>();
        var property = CreateProperty(new VectorPropertyModel("Embedding", typeof(string)), generator, dimensions: 256);

        await property.GenerateEmbeddingsAsync(["a", "b"], CancellationToken.None);

        Assert.Equal(256, Assert.Single(generator.ReceivedOptions)?.Dimensions);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_with_custom_input_type_passes_Dimensions_to_generator()
    {
        var generator = new RecordingEmbeddingGenerator<int>();
        var property = CreateProperty(new VectorPropertyModel<int>("Embedding"), generator, dimensions: 128);

        await property.GenerateEmbeddingAsync(42, CancellationToken.None);
        await property.GenerateEmbeddingsAsync(new object?[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(2, generator.ReceivedOptions.Count);
        Assert.All(generator.ReceivedOptions, o => Assert.Equal(128, o?.Dimensions));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_without_Dimensions_passes_null_options()
    {
        var generator = new RecordingEmbeddingGenerator<string>();
        var property = CreateProperty(new VectorPropertyModel("Embedding", typeof(string)), generator, dimensions: null);

        await property.GenerateEmbeddingAsync("text", CancellationToken.None);

        Assert.Null(Assert.Single(generator.ReceivedOptions));
    }

    private static VectorPropertyModel CreateProperty(VectorPropertyModel property, IEmbeddingGenerator generator, int? dimensions)
    {
        if (dimensions is { } d)
        {
            property.Dimensions = d;
        }

        property.EmbeddingGenerator = generator;
        property.EmbeddingType = typeof(Embedding<float>);
        property.EmbeddingGenerationDispatcher = EmbeddingGenerationDispatcher.Create<Embedding<float>>();
        return property;
    }

    private sealed class RecordingEmbeddingGenerator<TInput> : IEmbeddingGenerator<TInput, Embedding<float>>
    {
        public List<EmbeddingGenerationOptions?> ReceivedOptions { get; } = [];

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            ReceivedOptions.Add(options);
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                values.Select(_ => new Embedding<float>(new float[options?.Dimensions ?? 3]))));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

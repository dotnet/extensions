// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public sealed class EmbeddingGeneratorBuilder<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The registered client factory instances.</summary>
    private List<Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>>>? _generatorFactories;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> class.</summary>
    /// <param name="services">The service provider to use for dependency injection.</param>
    public EmbeddingGeneratorBuilder(IServiceProvider? services = null)
    {
        Services = services ?? EmptyServiceProvider.Instance;
    }

    /// <summary>Gets the <see cref="IServiceProvider"/> associated with the builder instance.</summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Builds an instance of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> using the specified inner generator.
    /// </summary>
    /// <param name="innerGenerator">The inner generator to use.</param>
    /// <returns>An instance of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</returns>
    /// <remarks>
    /// If there are any factories registered with this builder, <paramref name="innerGenerator"/> is used as a seed to
    /// the last factory, and the result of each factory delegate is passed to the previously registered factory.
    /// The final result is then returned from this call.
    /// </remarks>
    public IEmbeddingGenerator<TInput, TEmbedding> Use(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        var embeddingGenerator = Throw.IfNull(innerGenerator);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_generatorFactories is not null)
        {
            for (var i = _generatorFactories.Count - 1; i >= 0; i--)
            {
                embeddingGenerator = _generatorFactories[i](Services, embeddingGenerator) ??
                    throw new InvalidOperationException(
                        $"The {nameof(IEmbeddingGenerator<TInput, TEmbedding>)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IEmbeddingGenerator<TInput, TEmbedding>)} instances.");
            }
        }

        return embeddingGenerator;
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(Func<IEmbeddingGenerator<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        return Use((_, innerGenerator) => generatorFactory(innerGenerator));
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        _generatorFactories ??= [];
        _generatorFactories.Add(generatorFactory);
        return this;
    }
}

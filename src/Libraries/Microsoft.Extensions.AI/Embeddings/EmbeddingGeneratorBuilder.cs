// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public sealed class EmbeddingGeneratorBuilder<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private delegate IEmbeddingGenerator<TInput, TEmbedding> EmbeddingGeneratorFactory(IFeatureCollection features, IServiceProvider services);

    private readonly Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> _innerGeneratorFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<EmbeddingGeneratorFactory, EmbeddingGeneratorFactory>>? _generatorMiddleware;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The inner <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        _ = Throw.IfNull(innerGenerator);
        _innerGeneratorFactory = _ => innerGenerator;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    public EmbeddingGeneratorBuilder(Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory)
    {
        _innerGeneratorFactory = Throw.IfNull(innerGeneratorFactory);
    }

    /// <summary>
    /// Builds an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the entire pipeline.</returns>
    public IEmbeddingGenerator<TInput, TEmbedding> Build(IServiceProvider? services = null)
    {
        EmbeddingGeneratorFactory embeddingGeneratorFactory = (f, s) => _innerGeneratorFactory(s);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_generatorMiddleware is not null)
        {
            for (var i = _generatorMiddleware.Count - 1; i >= 0; i--)
            {
                embeddingGeneratorFactory = _generatorMiddleware[i](embeddingGeneratorFactory);
                if (embeddingGeneratorFactory is null)
                {
                    Throw.InvalidOperationException(
                        $"The {nameof(EmbeddingGeneratorBuilder<TInput, TEmbedding>)} middleware entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(Func<IFeatureCollection, IEmbeddingGenerator<TInput, TEmbedding>>)} instances.");
                }
            }
        }

        var features = new FeatureCollection();
        services ??= EmptyServiceProvider.Instance;
        var embeddingGenerator = embeddingGeneratorFactory(features, services);

        if (embeddingGenerator is null)
        {
            Throw.InvalidOperationException(
                $"The {nameof(EmbeddingGeneratorBuilder<TInput, TEmbedding>)} middleware pipeline returned null." +
                $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IEmbeddingGenerator<TInput, TEmbedding>)} instances.");
        }

        return embeddingGenerator;
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorMiddleware">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorMiddleware"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(
        Func<Func<IFeatureCollection, IEmbeddingGenerator<TInput, TEmbedding>>, IServiceProvider,
                Func<IFeatureCollection, IEmbeddingGenerator<TInput, TEmbedding>>> generatorMiddleware)
    {
        _ = Throw.IfNull(generatorMiddleware);

        _generatorMiddleware ??= [];
        _generatorMiddleware.Add(nextFactory =>
        {
            return (features, services) =>
            {
                var appliedMiddleware = generatorMiddleware(fc => nextFactory(fc, services), services);
                return appliedMiddleware(features);
            };
        });

        return this;
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(Func<IEmbeddingGenerator<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        return Use((innerGenerator, _) => generatorFactory(innerGenerator));
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(
        Func<IEmbeddingGenerator<TInput, TEmbedding>, IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        return Use((nextFactory, services) =>
        {
            return features =>
            {
                var nextGenerator = nextFactory(features);
                return generatorFactory(nextGenerator, services);
            };
        });
    }

    /// <summary>
    /// Adds to the embedding generator pipeline an anonymous delegating embedding generator based on a delegate that provides
    /// an implementation for <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/>.
    /// </summary>
    /// <param name="generateFunc">
    /// A delegate that provides the implementation for <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/>.
    /// </param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generateFunc"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(
        Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>>? generateFunc)
    {
        _ = Throw.IfNull(generateFunc);

        return Use((innerGenerator, _) => new AnonymousDelegatingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, generateFunc));
    }
}

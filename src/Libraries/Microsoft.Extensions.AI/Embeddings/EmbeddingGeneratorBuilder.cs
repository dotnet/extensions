// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Adds a callback that configures a <see cref="EmbeddingGenerationOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed a clone of the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance (or a new constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="EmbeddingGenerationOptions"/> if the caller didn't supply a <see cref="EmbeddingGenerationOptions"/> instance, or
    /// a clone (via <see cref="EmbeddingGenerationOptions.Clone"/>
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The current builder instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> ConfigureOptions(
        Action<EmbeddingGenerationOptions> configure)
    {
        _ = Throw.IfNull(configure);

        return Use(innerGenerator => new ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, configure));
    }

    /// <summary>
    /// Adds a <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> as the next stage in the pipeline.
    /// </summary>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> UseDistributedCache(
        IDistributedCache? storage = null,
        Action<DistributedCachingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
    {
        return Use((services, innerGenerator) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var result = new DistributedCachingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, storage);
            configure?.Invoke(result);
            return result;
        });
    }

    /// <summary>Adds logging to the embedding generator pipeline.</summary>
    /// <param name="logger">
    /// An optional <see cref="ILogger"/> with which logging should be performed. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> UseLogging(
        ILogger? logger = null, Action<LoggingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
    {
        return Use((services, innerGenerator) =>
        {
            logger ??= services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LoggingEmbeddingGenerator<TInput, TEmbedding>));
            var generator = new LoggingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, logger);
            configure?.Invoke(generator);
            return generator;
        });
    }

    /// <summary>
    /// Adds OpenTelemetry support to the embedding generator pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this generator is also subject to change.
    /// </remarks>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> UseOpenTelemetry(
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
    {
        return Use((services, innerGenerator) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var generator = new OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>(
                innerGenerator,
                loggerFactory?.CreateLogger(typeof(OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>)),
                sourceName);
            configure?.Invoke(generator);
            return generator;
        });
    }
}

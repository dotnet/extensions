// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class OpenTelemetryEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the embedding generator pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <https://opentelemetry.io/docs/specs/semconv/gen-ai/>.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this generator is also subject to change.
    /// </remarks>
    /// <typeparam name="TInput">The type of input used to produce embeddings.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding generated.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseOpenTelemetry<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding =>
        Throw.IfNull(builder).Use((services, innerGenerator) =>
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

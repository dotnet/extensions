// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class LoggingEmbeddingGeneratorBuilderExtensions
{
    /// <summary>Adds logging to the embedding generator pipeline.</summary>
    /// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseLogging<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerGenerator, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();
            var generator = new LoggingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, loggerFactory.CreateLogger(typeof(LoggingEmbeddingGenerator<TInput, TEmbedding>)));
            configure?.Invoke(generator);
            return generator;
        });
    }
}

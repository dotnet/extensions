// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    /// If not supplied, an instance will be resolved from the service provider.
    /// If no instance is available, no logging will be performed.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// The resulting logging will be to an <see cref="ILogger"/> created by the <paramref name="loggerFactory"/>,
    /// or if no <paramref name="loggerFactory"/> is supplied, by a <see cref="ILoggerFactory"/> queried from
    /// the services in <paramref name="builder"/>. If no <see cref="ILoggerFactory"/> is available, no logging
    /// will be performed.
    /// </remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseLogging<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerGenerator, services) =>
        {
            // If no factory was specified, try to resolve one from the service provider.
            // Then if we still couldn't get one, or if we got the null logger factory,
            // there's no point in creating a logging client, as it'll be a nop, so just
            // skip it. As an alternative design, this could throw an exception, but that
            // then leads consumers to do this check on their own, querying the service provider
            // to see if it includes a logger factory and only calling UseLogging if it does,
            // which both negates the fluent API and duplicates the check done here.
            loggerFactory ??= services.GetService<ILoggerFactory>();
            if (loggerFactory is null || loggerFactory == NullLoggerFactory.Instance)
            {
                return innerGenerator;
            }

            var generator = new LoggingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, loggerFactory.CreateLogger(typeof(LoggingEmbeddingGenerator<TInput, TEmbedding>)));
            configure?.Invoke(generator);
            return generator;
        });
    }
}

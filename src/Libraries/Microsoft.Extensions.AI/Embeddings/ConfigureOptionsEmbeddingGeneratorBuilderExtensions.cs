// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class ConfigureOptionsEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="EmbeddingGenerationOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
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
    /// <returns>The <paramref name="builder"/>.</returns>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> ConfigureOptions<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        Action<EmbeddingGenerationOptions> configure)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerGenerator => new ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, configure));
    }
}

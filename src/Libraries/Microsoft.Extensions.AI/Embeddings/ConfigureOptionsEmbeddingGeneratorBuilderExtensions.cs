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
    /// Adds a callback that updates or replaces <see cref="EmbeddingGenerationOptions"/>. This can be used to set default options.
    /// </summary>
    /// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="configureOptions">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance and should return the configured <see cref="EmbeddingGenerationOptions"/> instance to use.
    /// </param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// The configuration callback is invoked with the caller-supplied <see cref="EmbeddingGenerationOptions"/> instance. To override the caller-supplied options
    /// with a new instance, the callback may simply return that new instance, for example <c>_ => new EmbeddingGenerationOptions() { Dimensions = 100 }</c>. To provide
    /// a new instance only if the caller-supplied instance is `null`, the callback may conditionally return a new instance, for example
    /// <c>options => options ?? new EmbeddingGenerationOptions() { Dimensions = 100 }</c>. Any changes to the caller-provided options instance will persist on the
    /// original instance, so the callback must take care to only do so when such mutations are acceptable, such as by cloning the original instance
    /// and mutating the clone, for example:
    /// <c>
    /// options =>
    /// {
    ///     var newOptions = options?.Clone() ?? new();
    ///     newOptions.Dimensions = 100;
    ///     return newOptions;
    /// }
    /// </c>
    /// </remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseEmbeddingGenerationOptions<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        Func<EmbeddingGenerationOptions?, EmbeddingGenerationOptions?> configureOptions)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configureOptions);

        return builder.Use(innerGenerator => new ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, configureOptions));
    }
}

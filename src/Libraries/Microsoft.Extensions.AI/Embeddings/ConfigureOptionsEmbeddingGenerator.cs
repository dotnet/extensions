// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that configures a <see cref="EmbeddingGenerationOptions"/> instance used by the remainder of the pipeline.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
public sealed class ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<EmbeddingGenerationOptions> _configureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureOptionsEmbeddingGenerator{TInput, TEmbedding}"/> class with the
    /// specified <paramref name="configure"/> callback.
    /// </summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed a clone of the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance (or a newly-constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="EmbeddingGenerationOptions"/> if
    /// the caller didn't supply a <see cref="EmbeddingGenerationOptions"/> instance, or a clone (via <see cref="EmbeddingGenerationOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        Action<EmbeddingGenerationOptions> configure)
        : base(innerGenerator)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await base.GenerateAsync(values, Configure(options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates and configures the <see cref="EmbeddingGenerationOptions"/> to pass along to the inner client.</summary>
    private EmbeddingGenerationOptions Configure(EmbeddingGenerationOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}

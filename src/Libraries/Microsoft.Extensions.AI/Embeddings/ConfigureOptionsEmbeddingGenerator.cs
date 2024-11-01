// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that updates or replaces the <see cref="EmbeddingGenerationOptions"/> used by the remainder of the pipeline.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
/// <remarks>
/// <para>
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
/// </para>
/// <para>
/// The provided implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> is thread-safe for concurrent use so long as the employed configuration
/// callback is also thread-safe for concurrent requests. If callers employ a shared options instance, care should be taken in the
/// configuration callback, as multiple calls to it may end up running in parallel with the same options instance.
/// </para>
/// </remarks>
public sealed class ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Func<EmbeddingGenerationOptions?, EmbeddingGenerationOptions?> _configureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureOptionsEmbeddingGenerator{TInput, TEmbedding}"/> class with the
    /// specified <paramref name="configureOptions"/> callback.
    /// </summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="configureOptions">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance and should return the configured <see cref="EmbeddingGenerationOptions"/> instance to use.
    /// </param>
    public ConfigureOptionsEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        Func<EmbeddingGenerationOptions?, EmbeddingGenerationOptions?> configureOptions)
        : base(innerGenerator)
    {
        _configureOptions = Throw.IfNull(configureOptions);
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await base.GenerateAsync(values, _configureOptions(options), cancellationToken).ConfigureAwait(false);
    }
}

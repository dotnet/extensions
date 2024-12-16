// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that wraps an inner generator with implementations provided by delegates.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public sealed class AnonymousDelegatingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The delegate to use as the implementation of <see cref="GenerateAsync"/>.</summary>
    private readonly Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>> _generateFunc;

    /// <summary>Initializes a new instance of the <see cref="AnonymousDelegatingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="generateFunc">A delegate that provides the implementation for <see cref="GenerateAsync"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="generateFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>> generateFunc)
        : base(innerGenerator)
    {
        _ = Throw.IfNull(generateFunc);

        _generateFunc = generateFunc;
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        return await _generateFunc(values, options, InnerGenerator, cancellationToken).ConfigureAwait(false);
    }
}

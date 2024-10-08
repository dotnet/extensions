// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that passes through calls to another instance.
/// </summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
/// <remarks>
/// This is recommended as a base type when building generators that can be chained in any order around an underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
/// The default implementation simply passes each call to the inner generator instance.
/// </remarks>
public class DelegatingEmbeddingGenerator<TInput, TEmbedding> : IEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingEmbeddingGenerator{TInput, TEmbedding}"/> class.
    /// </summary>
    /// <param name="innerGenerator">The wrapped generator instance.</param>
    protected DelegatingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        InnerGenerator = Throw.IfNull(innerGenerator);
    }

    /// <summary>Gets the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}" />.</summary>
    protected IEmbeddingGenerator<TInput, TEmbedding> InnerGenerator { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing">true if being called from <see cref="Dispose()"/>; otherwise, false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerGenerator.Dispose();
        }
    }

    /// <inheritdoc />
    public virtual EmbeddingGeneratorMetadata Metadata =>
        InnerGenerator.Metadata;

    /// <inheritdoc />
    public virtual Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerGenerator.GenerateAsync(values, options, cancellationToken);

    /// <inheritdoc />
    public virtual TService? GetService<TService>(object? key = null)
        where TService : class
    {
#pragma warning disable S3060 // "is" should not be used with "this"
        // If the key is non-null, we don't know what it means so pass through to the inner service
        return key is null && this is TService service ? service : InnerGenerator.GetService<TService>(key);
#pragma warning restore S3060
    }
}

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
/// <typeparam name="TInput">The type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">The type of the embedding instance produced by the generator.</typeparam>
/// <remarks>
/// This type is recommended as a base type when building generators that can be chained in any order around an underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
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

    /// <inheritdoc />
    public virtual Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerGenerator.GenerateAsync(values, options, cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerGenerator.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerGenerator.Dispose();
        }
    }
}

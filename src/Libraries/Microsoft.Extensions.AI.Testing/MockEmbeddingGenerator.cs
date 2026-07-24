// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A configurable <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for tests and local mock scenarios.
/// </summary>
/// <typeparam name="TInput">The type of input accepted by the embedding generator.</typeparam>
/// <remarks>
/// Configure <see cref="GenerateAsyncCallback"/> to produce the embeddings required by a test. The callback receives
/// the original input enumerable and cancellation token. <see cref="CallCount"/> records every generation request.
/// </remarks>
public class MockEmbeddingGenerator<TInput> : IEmbeddingGenerator<TInput, Embedding<float>>
{
    private int _callCount;

    /// <summary>Initializes a new instance of the <see cref="MockEmbeddingGenerator{TInput}"/> class.</summary>
    public MockEmbeddingGenerator()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    /// <summary>Gets or sets the callback that generates embeddings.</summary>
    /// <remarks>
    /// The callback must be set before calling <see cref="GenerateAsync(IEnumerable{TInput}, EmbeddingGenerationOptions?, CancellationToken)"/>.
    /// </remarks>
    public Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>>? GenerateAsyncCallback { get; set; }

    /// <summary>Gets or sets the callback that resolves services.</summary>
    /// <remarks>
    /// By default, this callback returns this generator for compatible, non-keyed requests and <see langword="null"/>
    /// for all other requests.
    /// </remarks>
    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    /// <summary>Gets the number of calls to <see cref="GenerateAsync(IEnumerable{TInput}, EmbeddingGenerationOptions?, CancellationToken)"/>.</summary>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <inheritdoc />
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);
        RecordCall();

        return GenerateCoreAsync(values, options, cancellationToken);
    }

    /// <summary>Generates embeddings after the request has been recorded.</summary>
    /// <param name="values">The values to embed.</param>
    /// <param name="options">The generation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated embeddings.</returns>
    protected virtual Task<GeneratedEmbeddings<Embedding<float>>> GenerateCoreAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        if (GenerateAsyncCallback is not { } callback)
        {
            throw new InvalidOperationException("No embedding generation callback has been configured.");
        }

        return Throw.IfNull(callback(values, options, cancellationToken));
    }

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);
        return Throw.IfNull(GetServiceCallback)(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources used by this mock generator.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private void RecordCall() => Interlocked.Increment(ref _callCount);

    private MockEmbeddingGenerator<TInput>? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

namespace Microsoft.Extensions.AI;

public class TestEmbeddingGenerator<TInput, TEmbedding> : IEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    public TestEmbeddingGenerator()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>>? GenerateAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        => GenerateAsyncCallback!.Invoke(values, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    void IDisposable.Dispose()
    {
        // No resources to dispose
    }
}

public sealed class TestEmbeddingGenerator : TestEmbeddingGenerator<string, Embedding<float>>;

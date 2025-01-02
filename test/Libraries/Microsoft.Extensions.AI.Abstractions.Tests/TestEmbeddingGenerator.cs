// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public TestEmbeddingGenerator()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public EmbeddingGeneratorMetadata Metadata { get; set; } = new();

    public Func<IEnumerable<string>, EmbeddingGenerationOptions?, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>>? GenerateAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        => GenerateAsyncCallback!.Invoke(values, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    void IDisposable.Dispose()
    {
        // No resources to dispose
    }
}

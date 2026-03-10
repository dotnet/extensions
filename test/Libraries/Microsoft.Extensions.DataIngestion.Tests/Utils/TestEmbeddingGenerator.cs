// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.DataIngestion;

public sealed class TestEmbeddingGenerator<T> : IEmbeddingGenerator<T, Embedding<float>>
{
    public const int DimensionCount = 4;

    public bool WasCalled { get; private set; }

    public void Dispose()
    {
        // No resources to dispose
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<T> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        WasCalled = true;

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
            [new(new float[] { 0, 1, 2, 3 })]));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

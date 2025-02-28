// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Extensions.AI;

internal sealed class CallCountingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
    : DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
{
    private int _callCount;

    public int CallCount => _callCount;

    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return base.GenerateAsync(values, options, cancellationToken);
    }
}

internal static class CallCountingEmbeddingGeneratorBuilderExtensions
{
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> UseCallCounting(
        this EmbeddingGeneratorBuilder<string, Embedding<float>> builder) =>
        builder.Use(innerGenerator => new CallCountingEmbeddingGenerator(innerGenerator));
}

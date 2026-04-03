// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.VectorData.ProviderServices;

/// <summary>
/// A <see cref="EmbeddingGenerationDispatcher"/> implementation for a specific <typeparamref name="TEmbedding"/> type.
/// This is an internal support type meant for use by providers only and not by applications.
/// </summary>
/// <typeparam name="TEmbedding">The embedding type.</typeparam>
[Experimental("MEVD9001")]
public sealed class EmbeddingGenerationDispatcher<TEmbedding> : EmbeddingGenerationDispatcher
    where TEmbedding : Embedding
{
    /// <inheritdoc />
    public override Type EmbeddingType => typeof(TEmbedding);

    /// <inheritdoc />
    public override Type? ResolveEmbeddingType(VectorPropertyModel vectorProperty, IEmbeddingGenerator embeddingGenerator, Type? userRequestedEmbeddingType)
        => vectorProperty.ResolveEmbeddingType<TEmbedding>(embeddingGenerator, userRequestedEmbeddingType);

    /// <inheritdoc />
    public override bool CanGenerateEmbedding(VectorPropertyModel vectorProperty, IEmbeddingGenerator embeddingGenerator)
        => vectorProperty.CanGenerateEmbedding<TEmbedding>(embeddingGenerator);

    /// <inheritdoc />
    public override Task<IReadOnlyList<Embedding>> GenerateEmbeddingsAsync(VectorPropertyModel vectorProperty, IEnumerable<object?> values, CancellationToken cancellationToken)
        => vectorProperty.GenerateEmbeddingsCoreAsync<TEmbedding>(values, cancellationToken);

    /// <inheritdoc />
    public override Task<Embedding> GenerateEmbeddingAsync(VectorPropertyModel vectorProperty, object? value, CancellationToken cancellationToken)
        => vectorProperty.GenerateEmbeddingCoreAsync<TEmbedding>(value, cancellationToken);
}

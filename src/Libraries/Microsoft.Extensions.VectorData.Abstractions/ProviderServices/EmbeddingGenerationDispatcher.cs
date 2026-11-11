// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.VectorData.ProviderServices;

/// <summary>
/// Represents a supported embedding type for a vector store provider.
/// This is an internal support type meant for use by providers only and not by applications.
/// </summary>
/// <remarks>
/// Each instance encapsulates both build-time embedding type resolution and runtime embedding generation
/// for a specific <see cref="Embedding"/> subtype.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.VectorDataProviderServices, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class EmbeddingGenerationDispatcher
{
    /// <summary>
    /// Creates a new <see cref="EmbeddingGenerationDispatcher"/> for the given <typeparamref name="TEmbedding"/> type.
    /// </summary>
    /// <typeparam name="TEmbedding">The embedding type.</typeparam>
    /// <returns>A new <see cref="EmbeddingGenerationDispatcher"/> instance.</returns>
    public static EmbeddingGenerationDispatcher Create<TEmbedding>()
        where TEmbedding : Embedding
        => new EmbeddingGenerationDispatcher<TEmbedding>();

    /// <summary>
    /// Gets the <see cref="Embedding"/> type that this instance supports.
    /// </summary>
    public abstract Type EmbeddingType { get; }

    /// <summary>
    /// Attempts to resolve the embedding type for the given <paramref name="vectorProperty"/>, using the given <paramref name="embeddingGenerator"/>.
    /// </summary>
    /// <returns>The resolved embedding type, or <see langword="null"/> if the generator does not support this embedding type.</returns>
    public abstract Type? ResolveEmbeddingType(VectorPropertyModel vectorProperty, IEmbeddingGenerator embeddingGenerator, Type? userRequestedEmbeddingType);

    /// <summary>
    /// Generates embeddings of this type from the given <paramref name="values"/>, using the embedding generator configured on the <paramref name="vectorProperty"/>.
    /// </summary>
    /// <returns>The generated embeddings.</returns>
    public abstract Task<IReadOnlyList<Embedding>> GenerateEmbeddingsAsync(VectorPropertyModel vectorProperty, IEnumerable<object?> values, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a single embedding of this type from the given <paramref name="value"/>, using the embedding generator configured on the <paramref name="vectorProperty"/>.
    /// </summary>
    /// <returns>The generated embedding.</returns>
    public abstract Task<Embedding> GenerateEmbeddingAsync(VectorPropertyModel vectorProperty, object? value, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether the given <paramref name="embeddingGenerator"/> can produce embeddings of this type for any of the input types
    /// supported by the given <paramref name="vectorProperty"/>.
    /// This is used for native vector property types (e.g., <see cref="ReadOnlyMemory{T}"/> of <see langword="float"/>), where embedding generation
    /// is only needed for search and the input type is not known at model-build time.
    /// </summary>
    /// <returns><see langword="true"/> if the generator can produce embeddings of this type; otherwise, <see langword="false"/>.</returns>
    public abstract bool CanGenerateEmbedding(VectorPropertyModel vectorProperty, IEmbeddingGenerator embeddingGenerator);
}

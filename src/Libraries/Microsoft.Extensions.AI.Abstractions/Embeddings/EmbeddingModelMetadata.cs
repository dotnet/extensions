// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about a model used with an <see cref="IEmbeddingGenerator"/>.</summary>
public class EmbeddingModelMetadata
{
    /// <summary>Gets the number of dimensions in the embeddings produced by this model.</summary>
    /// <remarks>
    /// This value can be null if either the number of dimensions is unknown or there are multiple possible lengths associated with this model.
    /// An individual request may attempt to override this value via <see cref="EmbeddingGenerationOptions.Dimensions"/>.
    /// </remarks>
    public int? Dimensions { get; init; }
}

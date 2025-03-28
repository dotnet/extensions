// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
public class EmbeddingGeneratorMetadata
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the embedding generation provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the embedding generation provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the default embedding generation model used, if applicable.</param>
    /// <param name="defaultModelDimensions">The number of dimensions in vectors produced by the default model, if applicable.</param>
    public EmbeddingGeneratorMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null, int? defaultModelDimensions = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
        DefaultModelDimensions = defaultModelDimensions;
    }

    /// <summary>Gets the name of the embedding generation provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the embedding generation provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this embedding generator.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding embedding generator.
    /// An individual request may override this value via <see cref="EmbeddingGenerationOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }

    /// <summary>Gets the number of dimensions in the embeddings produced by the default model.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if either the number of dimensions is unknown or there are multiple possible lengths associated with this model.
    /// An individual request may override this value via <see cref="EmbeddingGenerationOptions.Dimensions"/>.
    /// </remarks>
    public int? DefaultModelDimensions { get; }
}

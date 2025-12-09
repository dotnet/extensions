// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IImageGenerator"/>.</summary>
[Experimental(DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class ImageGeneratorMetadata
{
    /// <summary>Initializes a new instance of the <see cref="ImageGeneratorMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the image generation provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the image generation provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the image generation model used by default, if applicable.</param>
    public ImageGeneratorMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the image generation provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the image generation provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this image generator.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding <see cref="IImageGenerator"/>.
    /// An individual request may override this value via <see cref="ImageGenerationOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}

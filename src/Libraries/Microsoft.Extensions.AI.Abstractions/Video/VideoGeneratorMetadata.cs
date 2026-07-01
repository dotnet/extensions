// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IVideoGenerator"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class VideoGeneratorMetadata
{
    /// <summary>Initializes a new instance of the <see cref="VideoGeneratorMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the video generation provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the video generation provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the video generation model used by default, if applicable.</param>
    public VideoGeneratorMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the video generation provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the video generation provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this video generator.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding <see cref="IVideoGenerator"/>.
    /// An individual request may override this value via <see cref="VideoGenerationOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}

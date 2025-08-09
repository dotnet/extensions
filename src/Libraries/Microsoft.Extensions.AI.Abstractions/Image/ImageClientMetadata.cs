// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IImageClient"/>.</summary>
[Experimental("MEAI001")]
public class ImageClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="ImageClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the text-to-image provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the text-to-image provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the text-to-image model used by default, if applicable.</param>
    public ImageClientMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the text-to-image provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the text-to-image provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this text-to-image client.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding <see cref="IImageClient"/>.
    /// An individual request may override this value via <see cref="ImageOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}

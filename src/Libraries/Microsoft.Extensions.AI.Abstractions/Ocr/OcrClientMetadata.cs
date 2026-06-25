// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IOcrClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="OcrClientMetadata"/> class.</summary>
    /// <param name="providerName">The name of the OCR provider, if applicable.</param>
    /// <param name="providerUri">The URL for accessing the OCR provider, if applicable.</param>
    /// <param name="defaultModelId">The identifier of the model used by default, if applicable.</param>
    public OcrClientMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the OCR provider.</summary>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the OCR provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the identifier of the default model used by this OCR client.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if the name is unknown or if there are multiple possible
    /// models associated with this instance. An individual request can override this value via
    /// <see cref="OcrOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}

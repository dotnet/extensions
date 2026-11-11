// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="ITextToSpeechClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public class TextToSpeechClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="TextToSpeechClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the text to speech provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the text to speech provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the text to speech model used by default, if applicable.</param>
    public TextToSpeechClientMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the text to speech provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the text to speech provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this text to speech client.</summary>
    /// <remarks>
    /// This value can be null if either the name is unknown or there are multiple possible models associated with this instance.
    /// An individual request may override this value via <see cref="TextToSpeechOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}

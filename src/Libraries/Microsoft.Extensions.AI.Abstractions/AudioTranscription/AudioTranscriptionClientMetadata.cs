// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IAudioTranscriptionClient"/>.</summary>
public class AudioTranscriptionClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the audio transcription  provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the audio transcription  provider, if applicable.</param>
    /// <param name="modelId">The ID of the audio transcription  model used, if applicable.</param>
    public AudioTranscriptionClientMetadata(string? providerName = null, Uri? providerUri = null, string? modelId = null)
    {
        ModelId = modelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the audio transcription provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the audio transcription provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the model used by this audio transcription provider.</summary>
    /// <remarks>
    /// This value can be null if either the name is unknown or there are multiple possible models associated with this instance.
    /// An individual request may override this value via <see cref="AudioTranscriptionOptions.ModelId"/>.
    /// </remarks>
    public string? ModelId { get; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an audio transcription request.</summary>
public class AudioTranscriptionOptions
{
    private CultureInfo? _audioLanguage;

    /// <summary>Gets or sets the ID for the audio transcription.</summary>
    /// <remarks>Long running jobs may use this ID for status pooling.</remarks>
    public string? TranscriptionId { get; set; }

    /// <summary>Gets or sets the model ID for the audio transcription.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the language for the audio transcription.</summary>
    public string? AudioLanguage
    {
        get => _audioLanguage?.Name;
        set => _audioLanguage = value is null ? null : CultureInfo.GetCultureInfo(value);
    }

    /// <summary>Gets or sets the sample rate for the audio transcription.</summary>
    public int? AudioSampleRate { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="AudioTranscriptionOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="AudioTranscriptionOptions"/> instance.</returns>
    public virtual AudioTranscriptionOptions Clone()
    {
        AudioTranscriptionOptions options = new()
        {
            TranscriptionId = TranscriptionId,
            ModelId = ModelId,
            AudioLanguage = AudioLanguage,
            AudioSampleRate = AudioSampleRate,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };

        return options;
    }
}

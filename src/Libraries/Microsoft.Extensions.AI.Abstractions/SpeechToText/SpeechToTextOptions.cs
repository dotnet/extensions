// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an speech to text request.</summary>
[Experimental("MEAI001")]
public class SpeechToTextOptions
{
    /// <summary>Gets or sets the model ID for the speech to text.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the language of source speech.</summary>
    public string? SpeechLanguage { get; set; }

    /// <summary>Gets or sets the language for the target generated text.</summary>
    public string? TextLanguage { get; set; }

    /// <summary>Gets or sets the sample rate of the speech input audio.</summary>
    public int? SpeechSampleRate { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="SpeechToTextOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="SpeechToTextOptions"/> instance.</returns>
    public virtual SpeechToTextOptions Clone()
    {
        SpeechToTextOptions options = new()
        {
            ModelId = ModelId,
            SpeechLanguage = SpeechLanguage,
            TextLanguage = TextLanguage,
            SpeechSampleRate = SpeechSampleRate,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };

        return options;
    }
}

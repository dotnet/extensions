// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the embedding generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="ISpeechToTextClient" /> implementation may have its own representation of options.
    /// When <see cref="ISpeechToTextClient.GetTextAsync" /> or <see cref="ISpeechToTextClient.GetStreamingTextAsync"/>
    /// is invoked with an <see cref="SpeechToTextOptions" />, that implementation may convert the provided options into
    /// its own representation in order to use it while performing the operation. For situations where a consumer knows
    /// which concrete <see cref="ISpeechToTextClient" /> is being used and how it represents options, a new instance of that
    /// implementation-specific options type may be returned by this callback, for the <see cref="ISpeechToTextClient" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="SpeechToTextOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="SpeechToTextOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<ISpeechToTextClient, object?>? RawRepresentationFactory { get; set; }

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

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
    public virtual SpeechToTextOptions Clone() =>
        new()
        {
            AdditionalProperties = AdditionalProperties?.Clone(),
            ModelId = ModelId,
            RawRepresentationFactory = RawRepresentationFactory,
            SpeechLanguage = SpeechLanguage,
            SpeechSampleRate = SpeechSampleRate,
            TextLanguage = TextLanguage,
        };

    /// <summary>Merges the options specified by <paramref name="other"/> into this instance.</summary>
    /// <param name="other">The other options to be merged into this instance.</param>
    /// <remarks>
    /// Merging works by copying the values from <paramref name="other"/> into this instance.
    /// For properties of primitive types, like <see cref="SpeechLanguage"/> or <see cref="ModelId"/>,
    /// the value will be copied only if it is <see langword="null"/> on this instance. For properties of
    /// dictionary types, like <see cref="AdditionalProperties"/>, a shallow copy is performed on the entries from <paramref name="other"/>,
    /// adding them into the corresponding dictionary on this instance, but only if the key does not already exist in this
    /// instance's dictionary.
    /// </remarks>
    public virtual void Merge(SpeechToTextOptions? other)
    {
        if (other is null)
        {
            return;
        }

        ModelId ??= other.ModelId;
        SpeechLanguage ??= other.SpeechLanguage;
        TextLanguage ??= other.TextLanguage;
        SpeechSampleRate ??= other.SpeechSampleRate;

        if (other.AdditionalProperties is { Count: > 0 })
        {
            if (AdditionalProperties is null)
            {
                AdditionalProperties = other.AdditionalProperties.Clone();
            }
            else
            {
                foreach (var entry in other.AdditionalProperties)
                {
                    _ = AdditionalProperties.TryAdd(entry.Key, entry.Value);
                }
            }
        }

        if (other.RawRepresentationFactory is { } otherRrf)
        {
            RawRepresentationFactory = RawRepresentationFactory is { } originalRrf ?
                client => originalRrf(client) ?? otherRrf(client) :
                otherRrf;
        }
    }
}

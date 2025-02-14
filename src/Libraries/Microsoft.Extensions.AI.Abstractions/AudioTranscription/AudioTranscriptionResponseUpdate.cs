// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="IAudioTranscriptionClient"/>.
/// </summary>
/// <remarks>
/// <para><see cref="AudioTranscriptionResponseUpdate"/> is so named because it represents streaming updates
/// to an audio transcroption. As such, it is considered erroneous for multiple updates that are part
/// of the same audio to contain competing values. For example, some updates that are part of
/// the same audio may have a <see langword="null"/> value, and others may have a non-<see langword="null"/> value,
/// but all of those with a non-<see langword="null"/> value must have the same value (e.g. <see cref="AudioTranscriptionResponseUpdate.TranscriptionId"/>).
/// </para>
/// <para>
/// The relationship between <see cref="AudioTranscriptionResponse"/> and <see cref="AudioTranscriptionResponseUpdate"/> is
/// codified in the <see cref="AudioTranscriptionResponseUpdateExtensions.ToAudioTranscriptionCompletionAsync"/> and
/// <see cref="AudioTranscriptionResponse.ToStreamingAudioTranscriptionUpdates"/>, which enable bidirectional conversions
/// between the two. Note, however, that the conversion may be slightly lossy, for example if multiple updates
/// all have different <see cref="AudioTranscriptionResponseUpdate.RawRepresentation"/> objects whereas there's
/// only one slot for such an object available in <see cref="AudioTranscriptionResponse.RawRepresentation"/>.
/// </para>
/// </remarks>
public class AudioTranscriptionResponseUpdate
{
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public AudioTranscriptionResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionResponseUpdate"/> class.</summary>
    /// <param name="contents">The contents for this message.</param>
    public AudioTranscriptionResponseUpdate(IList<AIContent> contents)
    {
        _contents = Throw.IfNull(contents);
    }

    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionResponseUpdate"/> class.</summary>
    /// <param name="content">Content of the message.</param>
    public AudioTranscriptionResponseUpdate(string? content)
        : this(content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Gets or sets the zero-based index of the input list with which this update is associated in the streaming sequence.</summary>
    public int InputIndex { get; set; }

    /// <summary>Gets or sets the zero-based index of the resulting choice with which this update is associated in the streaming sequence.</summary>
    public int ChoiceIndex { get; set; }

    /// <summary>Gets or sets the kind of the transcription update.</summary>
    public AudioTranscriptionResponseUpdateKind Kind { get; set; } = AudioTranscriptionResponseUpdateKind.Transcribing;

    /// <summary>Gets or sets the ID of the transcription of which this update is a part.</summary>
    public string? TranscriptionId { get; set; }

    /// <summary>Gets or sets the start time of the audio segment associated with this update in relation to the input audio length.</summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>Gets or sets the end time of the audio segment associated with this update in relation to the input audio length.</summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>Gets or sets the model ID using in the creation of the audio transcription of which this update is a part.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation of the transcription update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="AudioTranscriptionResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets the text of the first <see cref="TextContent"/> instance in <see cref="Contents" />.
    /// </summary>
    /// <remarks>
    /// If there is no <see cref="TextContent"/> instance in <see cref="Contents" />, then the getter returns <see langword="null" />,
    /// and the setter adds a new <see cref="TextContent"/> instance with the provided value.
    /// </remarks>
    [JsonIgnore]
    public string? Text
    {
        get => Contents.FindFirst<TextContent>()?.Text;
        set
        {
            if (Contents.FindFirst<TextContent>() is { } textContent)
            {
                textContent.Text = value;
            }
            else if (value is not null)
            {
                Contents.Add(new TextContent(value));
            }
        }
    }

    /// <summary>Gets or sets the transcription content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Contents.ConcatText();
}

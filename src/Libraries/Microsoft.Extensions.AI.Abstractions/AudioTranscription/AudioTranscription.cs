// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a choice in an audio transcription.</summary>
public class AudioTranscription
{
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="AudioTranscription"/> class.</summary>
    [JsonConstructor]
    public AudioTranscription()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AudioTranscription"/> class.</summary>
    /// <param name="content">Content of the message.</param>
    public AudioTranscription(string? content)
        : this(content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AudioTranscription"/> class.</summary>
    /// <param name="contents">The contents for this message.</param>
    public AudioTranscription(
        IList<AIContent> contents)
    {
        _contents = Throw.IfNull(contents);
    }

    /// <summary>Gets or sets the start time of the audio transcription choice.</summary>
    /// <remarks>This represents the start of the transcription in relation to the original audio source length.</remarks>
    public TimeSpan? StartTime { get; set; }

    /// <summary>Gets or sets the end time of the audio transcription choice.</summary>
    /// <remarks>This represents the end of the transcription in relation to the original audio source length.</remarks>
    public TimeSpan? EndTime { get; set; }

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

    /// <summary>Gets or sets the zero-based index of the input list with which this choice is associated.</summary>
    public int InputIndex { get; set; }

    /// <summary>Gets or sets the raw representation of the audio transcription choice from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="AudioTranscription"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the message.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Contents.ConcatText();
}

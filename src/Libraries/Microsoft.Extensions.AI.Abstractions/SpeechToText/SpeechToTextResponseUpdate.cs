// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operators

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="ISpeechToTextClient"/>.
/// </summary>
/// <remarks>
/// <para><see cref="SpeechToTextResponseUpdate"/> is so named because it represents streaming updates
/// to an speech to text generation. As such, it is considered erroneous for multiple updates that are part
/// of the same audio speech to contain competing values. For example, some updates that are part of
/// the same audio speech may have a <see langword="null"/> value, and others may have a non-<see langword="null"/> value,
/// but all of those with a non-<see langword="null"/> value must have the same value (e.g. <see cref="SpeechToTextResponseUpdate.ResponseId"/>).
/// </para>
/// <para>
/// The relationship between <see cref="SpeechToTextResponse"/> and <see cref="SpeechToTextResponseUpdate"/> is
/// codified in the <see cref="SpeechToTextResponseUpdateExtensions.ToSpeechToTextResponseAsync"/> and
/// <see cref="SpeechToTextResponse.ToSpeechToTextResponseUpdates"/>, which enable bidirectional conversions
/// between the two. Note, however, that the conversion may be slightly lossy, for example if multiple updates
/// all have different <see cref="SpeechToTextResponseUpdate.RawRepresentation"/> objects whereas there's
/// only one slot for such an object available in <see cref="SpeechToTextResponse.RawRepresentation"/>.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public class SpeechToTextResponseUpdate
{
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public SpeechToTextResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponseUpdate"/> class.</summary>
    /// <param name="contents">The contents for this message.</param>
    public SpeechToTextResponseUpdate(IList<AIContent> contents)
    {
        _contents = Throw.IfNull(contents);
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponseUpdate"/> class.</summary>
    /// <param name="content">Content of the message.</param>
    public SpeechToTextResponseUpdate(string? content)
        : this(content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Gets or sets the kind of the generated text update.</summary>
    public SpeechToTextResponseUpdateKind Kind { get; set; } = SpeechToTextResponseUpdateKind.TextUpdating;

    /// <summary>Gets or sets the ID of the generated text response of which this update is a part.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the start time of the text segment associated with this update in relation to the full audio speech length.</summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>Gets or sets the end time of the text segment associated with this update in relation to the full audio speech length.</summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>Gets or sets the model ID using in the creation of the speech to text of which this update is a part.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation of the generated text update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="SpeechToTextResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets the text of this speech to text response.</summary>
    /// <remarks>
    /// This property concatenates the text of all <see cref="TextContent"/> objects in <see cref="Contents"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => _contents?.ConcatText() ?? string.Empty;

    /// <summary>Gets or sets the generated content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Text;
}

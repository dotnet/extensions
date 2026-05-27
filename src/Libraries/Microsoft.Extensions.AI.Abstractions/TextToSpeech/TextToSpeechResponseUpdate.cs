// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="ITextToSpeechClient"/>.
/// </summary>
/// <remarks>
/// <para><see cref="TextToSpeechResponseUpdate"/> is so named because it represents streaming updates
/// to a text to speech generation. As such, it is considered erroneous for multiple updates that are part
/// of the same request to contain competing values. For example, some updates that are part of
/// the same request may have a <see langword="null"/> value, and others may have a non-<see langword="null"/> value,
/// but all of those with a non-<see langword="null"/> value must have the same value (e.g. <see cref="TextToSpeechResponseUpdate.ResponseId"/>).
/// </para>
/// <para>
/// The relationship between <see cref="TextToSpeechResponse"/> and <see cref="TextToSpeechResponseUpdate"/> is
/// codified in the <see cref="TextToSpeechResponseUpdateExtensions.ToTextToSpeechResponseAsync"/> and
/// <see cref="TextToSpeechResponse.ToTextToSpeechResponseUpdates"/>, which enable bidirectional conversions
/// between the two. Note, however, that the conversion may be slightly lossy, for example if multiple updates
/// all have different <see cref="TextToSpeechResponseUpdate.RawRepresentation"/> objects whereas there's
/// only one slot for such an object available in <see cref="TextToSpeechResponse.RawRepresentation"/>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public class TextToSpeechResponseUpdate
{
    /// <summary>Initializes a new instance of the <see cref="TextToSpeechResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public TextToSpeechResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TextToSpeechResponseUpdate"/> class.</summary>
    /// <param name="contents">The contents for this update.</param>
    public TextToSpeechResponseUpdate(IList<AIContent> contents)
    {
        Contents = Throw.IfNull(contents);
    }

    /// <summary>Gets or sets the kind of the generated audio speech update.</summary>
    public TextToSpeechResponseUpdateKind Kind { get; set; } = TextToSpeechResponseUpdateKind.AudioUpdating;

    /// <summary>Gets or sets the ID of the generated audio speech response of which this update is a part.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the text to speech of which this update is a part.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation of the generated audio speech update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="TextToSpeechResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the generated content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => field ??= [];
        set;
    }
}

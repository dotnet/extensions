// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of a text to speech request.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public class TextToSpeechResponse
{
    /// <summary>Initializes a new instance of the <see cref="TextToSpeechResponse"/> class.</summary>
    [JsonConstructor]
    public TextToSpeechResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TextToSpeechResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    public TextToSpeechResponse(IList<AIContent> contents)
    {
        Contents = Throw.IfNull(contents);
    }

    /// <summary>Gets or sets the ID of the text to speech response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the text to speech response.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation of the text to speech response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="TextToSpeechResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the text to speech response.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Creates an array of <see cref="TextToSpeechResponseUpdate" /> instances that represent this <see cref="TextToSpeechResponse" />.</summary>
    /// <returns>An array of <see cref="TextToSpeechResponseUpdate" /> instances that may be used to represent this <see cref="TextToSpeechResponse" />.</returns>
    public TextToSpeechResponseUpdate[] ToTextToSpeechResponseUpdates()
    {
        IList<AIContent> contents = Contents;
        if (Usage is { } usage)
        {
            contents = [.. contents, new UsageContent(usage)];
        }

        TextToSpeechResponseUpdate update = new()
        {
            Contents = contents,
            AdditionalProperties = AdditionalProperties,
            RawRepresentation = RawRepresentation,
            Kind = TextToSpeechResponseUpdateKind.AudioUpdated,
            ResponseId = ResponseId,
            ModelId = ModelId,
        };

        return [update];
    }

    /// <summary>Gets or sets the generated content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => field ??= [];
        set;
    }

    /// <summary>Gets or sets usage details for the text to speech response.</summary>
    public UsageDetails? Usage { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operators

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an speech to text request.</summary>
[Experimental("MEAI001")]
public class SpeechToTextResponse
{
    /// <summary>The content items in the generated text response.</summary>
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponse"/> class.</summary>
    [JsonConstructor]
    public SpeechToTextResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    public SpeechToTextResponse(IList<AIContent> contents)
    {
        _contents = Throw.IfNull(contents);
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponse"/> class.</summary>
    /// <param name="content">Content of the response.</param>
    public SpeechToTextResponse(string? content)
        : this(content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Gets or sets the start time of the text segment in relation to the full audio speech length.</summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>Gets or sets the end time of the text segment in relation to the full audio speech length.</summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>Gets or sets the ID of the speech to text response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the speech to text completion.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation of the speech to text completion from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="SpeechToTextResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the speech to text completion.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets the text of this speech to text response.</summary>
    /// <remarks>
    /// This property concatenates the text of all <see cref="TextContent"/> objects in <see cref="Contents"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => _contents?.ConcatText() ?? string.Empty;

    /// <inheritdoc />
    public override string ToString() => Text;

    /// <summary>Creates an array of <see cref="SpeechToTextResponseUpdate" /> instances that represent this <see cref="SpeechToTextResponse" />.</summary>
    /// <returns>An array of <see cref="SpeechToTextResponseUpdate" /> instances that may be used to represent this <see cref="SpeechToTextResponse" />.</returns>
    public SpeechToTextResponseUpdate[] ToSpeechToTextResponseUpdates()
    {
        SpeechToTextResponseUpdate update = new SpeechToTextResponseUpdate
        {
            Contents = Contents,
            AdditionalProperties = AdditionalProperties,
            RawRepresentation = RawRepresentation,
            StartTime = StartTime,
            EndTime = EndTime,
            Kind = SpeechToTextResponseUpdateKind.TextUpdated,
            ResponseId = ResponseId,
            ModelId = ModelId,
        };

        return [update];
    }

    /// <summary>Gets or sets the generated content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }
}

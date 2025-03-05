// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an speech to text request.</summary>
public class SpeechToTextResponse
{
    /// <summary>The list of choices in the generated text response.</summary>
    private IList<SpeechToTextMessage> _choices;

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponse"/> class.</summary>
    /// <param name="message">the generated text representing the singular choice message in the response.</param>
    public SpeechToTextResponse(SpeechToTextMessage message)
        : this([Throw.IfNull(message)])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextResponse"/> class.</summary>
    /// <param name="choices">The list of choices in the response, one message per choice.</param>
    [JsonConstructor]
    public SpeechToTextResponse(IList<SpeechToTextMessage> choices)
    {
        _choices = Throw.IfNull(choices);
    }

    /// <summary>Gets the speech to text message details.</summary>
    /// <remarks>
    /// If no speech to text was generated, this property will throw.
    /// </remarks>
    [JsonIgnore]
    public SpeechToTextMessage Message
    {
        get
        {
            var choices = Choices;
            if (choices.Count == 0)
            {
                throw new InvalidOperationException($"The {nameof(SpeechToTextResponse)} instance does not contain any {nameof(SpeechToTextMessage)} choices.");
            }

            return choices[0];
        }
    }

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

    /// <inheritdoc />
    public override string ToString()
    {
        if (Choices.Count == 1)
        {
            return Choices[0].ToString();
        }

        StringBuilder sb = new();
        for (int i = 0; i < Choices.Count; i++)
        {
            if (i > 0)
            {
                _ = sb.AppendLine().AppendLine();
            }

            _ = sb.Append("Choice ").Append(i).AppendLine(":").Append(Choices[i]);
        }

        return sb.ToString();
    }

    /// <summary>Creates an array of <see cref="SpeechToTextResponseUpdate" /> instances that represent this <see cref="SpeechToTextResponse" />.</summary>
    /// <returns>An array of <see cref="SpeechToTextResponseUpdate" /> instances that may be used to represent this <see cref="SpeechToTextResponse" />.</returns>
    public SpeechToTextResponseUpdate[] ToSpeechToTextResponseUpdates()
    {
        SpeechToTextResponseUpdate? extra = null;
        if (AdditionalProperties is not null)
        {
            extra = new SpeechToTextResponseUpdate
            {
                Kind = SpeechToTextResponseUpdateKind.TextUpdated,
                AdditionalProperties = AdditionalProperties,
            };
        }

        int choicesCount = Choices.Count;
        var updates = new SpeechToTextResponseUpdate[choicesCount + (extra is null ? 0 : 1)];

        for (int choiceIndex = 0; choiceIndex < choicesCount; choiceIndex++)
        {
            SpeechToTextMessage choice = Choices[choiceIndex];
            updates[choiceIndex] = new SpeechToTextResponseUpdate
            {
                ChoiceIndex = choiceIndex,
                InputIndex = choice.InputIndex,

                AdditionalProperties = choice.AdditionalProperties,
                Contents = choice.Contents,
                RawRepresentation = choice.RawRepresentation,
                StartTime = choice.StartTime,
                EndTime = choice.EndTime,

                Kind = SpeechToTextResponseUpdateKind.TextUpdated,
                ResponseId = ResponseId,
                ModelId = ModelId,
            };
        }

        if (extra is not null)
        {
            updates[choicesCount] = extra;
        }

        return updates;
    }

    /// <summary>Gets or sets the list of speech to text choices.</summary>
    public IList<SpeechToTextMessage> Choices
    {
        get => _choices;
        set => _choices = Throw.IfNull(value);
    }
}

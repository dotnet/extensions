// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of a chat completion request.</summary>
public class ChatCompletion
{
    /// <summary>The list of choices in the completion.</summary>
    private IList<ChatMessage> _choices;

    /// <summary>Initializes a new instance of the <see cref="ChatCompletion"/> class.</summary>
    /// <param name="choices">The list of choices in the completion, one message per choice.</param>
    [JsonConstructor]
    public ChatCompletion(IList<ChatMessage> choices)
    {
        _choices = Throw.IfNull(choices);
    }

    /// <summary>Initializes a new instance of the <see cref="ChatCompletion"/> class.</summary>
    /// <param name="message">The chat message representing the singular choice in the completion.</param>
    public ChatCompletion(ChatMessage message)
    {
        _ = Throw.IfNull(message);
        _choices = [message];
    }

    /// <summary>Gets or sets the list of chat completion choices.</summary>
    public IList<ChatMessage> Choices
    {
        get => _choices;
        set => _choices = Throw.IfNull(value);
    }

    /// <summary>Gets the chat completion message.</summary>
    /// <remarks>
    /// If there are multiple choices, this property returns the first choice.
    /// If <see cref="Choices"/> is empty, this property will throw. Use <see cref="Choices"/> to access all choices directly.
    /// </remarks>
    [JsonIgnore]
    public ChatMessage Message
    {
        get
        {
            var choices = Choices;
            if (choices.Count == 0)
            {
                throw new InvalidOperationException($"The {nameof(ChatCompletion)} instance does not contain any {nameof(ChatMessage)} choices.");
            }

            return choices[0];
        }
    }

    /// <summary>Gets or sets the ID of the chat completion.</summary>
    public string? CompletionId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the chat completion.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets a timestamp for the chat completion.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the reason for the chat completion.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets usage details for the chat completion.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the raw representation of the chat completion from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatCompletion"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the chat completion.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        string.Join(Environment.NewLine, Choices);
}

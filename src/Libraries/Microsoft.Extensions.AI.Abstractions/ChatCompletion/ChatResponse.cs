// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a chat request.</summary>
public class ChatResponse
{
    /// <summary>The response message.</summary>
    private ChatMessage _message;

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    public ChatResponse()
    {
        _message = new(ChatRole.Assistant, []);
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="message">The response message.</param>
    public ChatResponse(ChatMessage message)
    {
        _ = Throw.IfNull(message);
        _message = message;
    }

    /// <summary>Gets or sets the chat response message.</summary>
    public ChatMessage Message
    {
        get => _message;
        set => _message = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the ID of the chat response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the chat thread ID associated with this chat response.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a chat thread, such that
    /// the input messages supplied to <see cref="IChatClient.GetResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ChatThreadId"/> instead of supplying the same messages
    /// (and this <see cref="ChatResponse"/>'s message) as part of the <c>chatMessages</c> parameter.
    /// </remarks>
    public string? ChatThreadId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the chat response.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets a timestamp for the chat response.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the reason for the chat response.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets usage details for the chat response.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the raw representation of the chat response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the chat response.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public override string ToString() => _message.ToString();

    /// <summary>Creates an array of <see cref="ChatResponseUpdate" /> instances that represent this <see cref="ChatResponse" />.</summary>
    /// <returns>An array of <see cref="ChatResponseUpdate" /> instances that may be used to represent this <see cref="ChatResponse" />.</returns>
    public ChatResponseUpdate[] ToChatResponseUpdates()
    {
        ChatResponseUpdate? extra = null;
        if (AdditionalProperties is not null || Usage is not null)
        {
            extra = new ChatResponseUpdate
            {
                AdditionalProperties = AdditionalProperties
            };

            if (Usage is { } usage)
            {
                extra.Contents.Add(new UsageContent(usage));
            }
        }

        var updates = new ChatResponseUpdate[extra is null ? 1 : 2];

        updates[0] = new ChatResponseUpdate
        {
            ChatThreadId = ChatThreadId,

            AdditionalProperties = _message.AdditionalProperties,
            AuthorName = _message.AuthorName,
            Contents = _message.Contents,
            RawRepresentation = _message.RawRepresentation,
            Role = _message.Role,

            ResponseId = ResponseId,
            CreatedAt = CreatedAt,
            FinishReason = FinishReason,
            ModelId = ModelId
        };

        if (extra is not null)
        {
            updates[1] = extra;
        }

        return updates;
    }
}

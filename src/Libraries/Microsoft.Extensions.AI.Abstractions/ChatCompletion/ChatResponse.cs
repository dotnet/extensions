// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a chat request.</summary>
/// <remarks>
/// <see cref="ChatResponse"/> provides one or more response messages and metadata about the response.
/// A typical response will contain a single message, however a response may contain multiple messages
/// in a variety of scenarios. For example, if automatic function calling is employed, such that a single
/// request to a <see cref="IChatClient"/> may actually generate multiple roundtrips to an inner <see cref="IChatClient"/>
/// it uses, all of the involved messages may be surfaced as part of the final <see cref="ChatResponse"/>.
/// </remarks>
public class ChatResponse
{
    /// <summary>The response messages.</summary>
    private IList<ChatMessage>? _messages;

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    public ChatResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="message">The response message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public ChatResponse(ChatMessage message)
    {
        _ = Throw.IfNull(message);

        Messages.Add(message);
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="messages">The response messages.</param>
    public ChatResponse(IList<ChatMessage>? messages)
    {
        _messages = messages;
    }

    /// <summary>Gets or sets the chat response messages.</summary>
    [AllowNull]
    public IList<ChatMessage> Messages
    {
        get => _messages ??= new List<ChatMessage>(1);
        set => _messages = value;
    }

    /// <summary>Gets the text of the response.</summary>
    /// <remarks>
    /// This property concatenates the <see cref="ChatMessage.Text"/> of all <see cref="ChatMessage"/>
    /// instances in <see cref="Messages"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => _messages?.ConcatText() ?? string.Empty;

    /// <summary>Gets or sets the ID of the chat response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets an identifier for the state of the conversation.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a conversation, such that
    /// the input messages supplied to <see cref="IChatClient.GetResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ConversationId"/> instead of supplying the same messages
    /// (and this <see cref="ChatResponse"/>'s message) as part of the <c>messages</c> parameter. Note that the value may
    /// or may not differ on every response, depending on whether the underlying provider uses a fixed ID for each conversation
    /// or updates it for each message.
    /// </remarks>
    /// <remarks>This method is obsolete. Use <see cref="ConversationId"/> instead.</remarks>
    [Obsolete("Use ConversationId instead.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public string? ChatThreadId
    {
        get => ConversationId;
        set => ConversationId = value;
    }

    /// <summary>Gets or sets an identifier for the state of the conversation.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a conversation, such that
    /// the input messages supplied to <see cref="IChatClient.GetResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ConversationId"/> instead of supplying the same messages
    /// (and this <see cref="ChatResponse"/>'s message) as part of the <c>messages</c> parameter. Note that the value may
    /// or may not differ on every response, depending on whether the underlying provider uses a fixed ID for each conversation
    /// or updates it for each message.
    /// </remarks>
    public string? ConversationId { get; set; }

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
    public override string ToString() => Text;

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

        int messageCount = _messages?.Count ?? 0;
        var updates = new ChatResponseUpdate[messageCount + (extra is not null ? 1 : 0)];

        int i;
        for (i = 0; i < messageCount; i++)
        {
            ChatMessage message = _messages![i];
            updates[i] = new ChatResponseUpdate
            {
                ConversationId = ConversationId,

                AdditionalProperties = message.AdditionalProperties,
                AuthorName = message.AuthorName,
                Contents = message.Contents,
                RawRepresentation = message.RawRepresentation,
                Role = message.Role,

                ResponseId = ResponseId,
                MessageId = message.MessageId,
                CreatedAt = CreatedAt,
                FinishReason = FinishReason,
                ModelId = ModelId
            };
        }

        if (extra is not null)
        {
            updates[i] = extra;
        }

        return updates;
    }
}

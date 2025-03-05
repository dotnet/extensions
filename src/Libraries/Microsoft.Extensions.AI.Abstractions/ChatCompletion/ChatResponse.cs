// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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
    public ChatResponse(ChatMessage? message)
    {
        if (message is not null)
        {
            Messages.Add(message);
        }
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="messages">The response messages.</param>
    public ChatResponse(IList<ChatMessage>? messages)
    {
        _messages = messages;
    }

    /// <summary>Gets or sets the chat response messages.</summary>
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
    public string Text
    {
        get
        {
            IList<ChatMessage>? messages = _messages;
            if (messages is null)
            {
                return string.Empty;
            }

            int count = messages.Count;
            return count switch
            {
                0 => string.Empty,
                1 => messages[0].Text,
                _ => string.Join(Environment.NewLine, messages.Select(m => m.Text).Where(s => !string.IsNullOrEmpty(s))),
            };
        }
    }

    /// <summary>Gets or sets the ID of the chat response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the chat thread ID associated with this chat response.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a chat thread, such that
    /// the input messages supplied to <see cref="IChatClient.GetResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ChatThreadId"/> instead of supplying the same messages
    /// (and this <see cref="ChatResponse"/>'s message) as part of the <c>messages</c> parameter.
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
                ChatThreadId = ChatThreadId,

                AdditionalProperties = message.AdditionalProperties,
                AuthorName = message.AuthorName,
                Contents = message.Contents,
                RawRepresentation = message.RawRepresentation,
                Role = message.Role,

                ResponseId = ResponseId,
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

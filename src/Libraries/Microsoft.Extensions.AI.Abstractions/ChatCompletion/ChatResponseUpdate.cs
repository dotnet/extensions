// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChatResponseUpdate"/> is so named because it represents updates
/// that layer on each other to form a single chat response. Conceptually, this combines the roles of
/// <see cref="ChatResponse"/> and <see cref="ChatMessage"/> in streaming output. For ease of consumption,
/// it also flattens the nested structure you see on streaming chunks in some AI services, so instead of a
/// dictionary of choices, each update is part of a single choice (and hence has its own role, choice ID, etc.).
/// </para>
/// <para>
/// The relationship between <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/> is
/// codified in the <see cref="ChatResponseUpdateExtensions.ToChatResponseAsync"/> and
/// <see cref="ChatResponse.ToChatResponseUpdates"/>, which enable bidirectional conversions
/// between the two. Note, however, that the provided conversions may be lossy, for example if multiple
/// updates all have different <see cref="RawRepresentation"/> objects whereas there's only one slot for
/// such an object available in <see cref="ChatResponse.RawRepresentation"/>. Similarly, if different
/// updates that are part of the same choice provide different values for properties like <see cref="ModelId"/>,
/// only one of the values will be used to populate <see cref="ChatResponse.ModelId"/>.
/// </para>
/// </remarks>
public class ChatResponseUpdate
{
    /// <summary>The response update content items.</summary>
    private IList<AIContent>? _contents;

    /// <summary>The name of the author of the update.</summary>
    private string? _authorName;

    /// <summary>Gets or sets the name of the author of the response update.</summary>
    public string? AuthorName
    {
        get => _authorName;
        set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Gets or sets the role of the author of the response update.</summary>
    public ChatRole? Role { get; set; }

    /// <summary>
    /// Gets or sets the text of the first <see cref="TextContent"/> instance in <see cref="Contents" />.
    /// </summary>
    /// <remarks>
    /// If there is no <see cref="TextContent"/> instance in <see cref="Contents" />, then the getter returns <see langword="null" />,
    /// and the setter will add new <see cref="TextContent"/> instance with the provided value.
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

    /// <summary>Gets or sets the chat response update content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <summary>Gets or sets the raw representation of the response update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the ID of the response of which this update is a part.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the chat thread ID associated with the chat response of which this update is a part.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a chat thread, such that
    /// the input messages supplied to <see cref="IChatClient.GetStreamingResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ChatThreadId"/> instead of supplying the same messages
    /// (and this streaming message) as part of the <c>chatMessages</c> parameter.
    /// </remarks>
    public string? ChatThreadId { get; set; }

    /// <summary>Gets or sets a timestamp for the response update.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the zero-based index of the choice with which this update is associated in the streaming sequence.</summary>
    public int ChoiceIndex { get; set; }

    /// <summary>Gets or sets the finish reason for the operation.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets the model ID associated with this response update.</summary>
    public string? ModelId { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Contents.ConcatText();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

// Conceptually this combines the roles of ChatCompletion and ChatMessage in streaming output.
// For ease of consumption, it also flattens the nested structure you see on streaming chunks in
// the OpenAI/Gemini APIs, so instead of a dictionary of choices, each update represents a single
// choice (and hence has its own role, choice ID, etc.).

/// <summary>
/// Represents a single response chunk from an <see cref="IChatClient"/>.
/// </summary>
public class StreamingChatCompletionUpdate
{
    /// <summary>The completion update content items.</summary>
    private IList<AIContent>? _contents;

    /// <summary>The name of the author of the update.</summary>
    private string? _authorName;

    /// <summary>Gets or sets the name of the author of the completion update.</summary>
    public string? AuthorName
    {
        get => _authorName;
        set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Gets or sets the role of the author of the completion update.</summary>
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
        get => Contents.OfType<TextContent>().FirstOrDefault()?.Text;
        set
        {
            if (Contents.OfType<TextContent>().FirstOrDefault() is { } textContent)
            {
                textContent.Text = value;
            }
            else if (value is not null)
            {
                Contents.Add(new TextContent(value));
            }
        }
    }

    /// <summary>Gets or sets the chat completion update content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <summary>Gets or sets the raw representation of the completion update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="StreamingChatCompletionUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the ID of the completion of which this update is a part.</summary>
    public string? CompletionId { get; set; }

    /// <summary>Gets or sets a timestamp for the completion update.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the zero-based index of the choice with which this update is associated in the streaming sequence.</summary>
    public int ChoiceIndex { get; set; }

    /// <summary>Gets or sets the finish reason for the operation.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets the model ID using in the creation of the chat completion of which this update is a part.</summary>
    public string? ModelId { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Text ?? string.Empty;
}

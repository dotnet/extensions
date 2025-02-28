﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a chat message used by an <see cref="IChatClient" />.</summary>
[DebuggerDisplay("[{Role}] {ContentForDebuggerDisplay}{EllipsesForDebuggerDisplay,nq}")]
public class ChatMessage
{
    private IList<AIContent>? _contents;
    private string? _authorName;

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    [JsonConstructor]
    public ChatMessage()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    /// <param name="role">The role of the author of the message.</param>
    /// <param name="content">The contents of the message.</param>
    public ChatMessage(ChatRole role, string? content)
        : this(role, content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    /// <param name="role">The role of the author of the message.</param>
    /// <param name="contents">The contents for this message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contents"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="contents"/> must not be read-only.</exception>
    public ChatMessage(
        ChatRole role,
        IList<AIContent> contents)
    {
        _ = Throw.IfNull(contents);
        _ = Throw.IfReadOnly(contents);

        Role = role;
        _contents = contents;
    }

    /// <summary>Clones the <see cref="ChatMessage"/> to a new <see cref="ChatMessage"/> instance.</summary>
    /// <returns>A shallow clone of the original message object.</returns>
    /// <remarks>
    /// This is a shallow clone. The returned instance is different from the original, but all properties
    /// refer to the same objects as the original.
    /// </remarks>
    public ChatMessage Clone() =>
        new()
        {
            AdditionalProperties = AdditionalProperties,
            _authorName = _authorName,
            _contents = _contents,
            RawRepresentation = RawRepresentation,
            Role = Role,
        };

    /// <summary>Gets or sets the name of the author of the message.</summary>
    public string? AuthorName
    {
        get => _authorName;
        set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Gets or sets the role of the author of the message.</summary>
    public ChatRole Role { get; set; } = ChatRole.User;

    /// <summary>
    /// Gets or sets the text of the first <see cref="TextContent"/> instance in <see cref="Contents" />.
    /// </summary>
    /// <remarks>
    /// If there is no <see cref="TextContent"/> instance in <see cref="Contents" />, then the getter returns <see langword="null" />,
    /// and the setter adds a new <see cref="TextContent"/> instance with the provided value.
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

    /// <summary>Gets or sets the chat message content items.</summary>
    /// <exception cref="ArgumentException">The <see cref="IList{T}"/> must not be read-only.</exception>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set
        {
            if (value is not null)
            {
                _ = Throw.IfReadOnly(value);
            }

            _contents = value;
        }
    }

    /// <summary>Gets or sets the raw representation of the chat message from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatMessage"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the message.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Contents.ConcatText();

    /// <summary>Gets a <see cref="AIContent"/> object to display in the debugger display.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private AIContent? ContentForDebuggerDisplay => _contents is { Count: > 0 } ? _contents[0] : null;

    /// <summary>Gets an indication for the debugger display of whether there's more content.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string EllipsesForDebuggerDisplay => _contents is { Count: > 1 } ? ", ..." : string.Empty;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time conversation item.
/// </summary>
/// <remarks>
/// This class is used to encapsulate the details of a real-time item that can be inserted into a conversation,
/// or sent as part of a real-time response creation process.
/// </remarks>
[Experimental("MEAI001")]
public class RealtimeContentItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeContentItem"/> class.
    /// </summary>
    /// <param name="id">The ID of the conversation item.</param>
    /// <param name="role">The role of the conversation item.</param>
    /// <param name="contents">The contents of the conversation item.</param>
    public RealtimeContentItem(IList<AIContent> contents, string? id = null, ChatRole? role = null)
    {
        Id = id;
        Role = role;
        Contents = contents;
    }

    /// <summary>
    /// Gets or sets the ID of the conversation item.
    /// </summary>
    /// <remarks>
    /// This ID can be null in case passing Function or MCP content where the ID is not required.
    /// The Id only needed of having contents representing a user, system, or assistant message with contents like text, audio, image or similar.
    /// </remarks>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the role of the conversation item.
    /// </summary>
    /// <remarks>
    /// The role not used in case of Function or MCP content.
    /// The role only needed of having contents representing a user, system, or assistant message with contents like text, audio, image or similar.
    /// </remarks>
    public ChatRole? Role { get; set; }

    /// <summary>
    /// Gets or sets the content of the conversation item.
    /// </summary>
    public IList<AIContent> Contents { get; set; }
}

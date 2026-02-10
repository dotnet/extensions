// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for creating a conversation item.
/// </summary>
[Experimental("MEAI001")]
public class RealtimeClientConversationItemCreateMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeClientConversationItemCreateMessage"/> class.
    /// </summary>
    /// <param name="item">The conversation item to create.</param>
    /// <param name="previousId">The optional ID of the previous conversation item to insert the new one after.</param>
    public RealtimeClientConversationItemCreateMessage(RealtimeContentItem item, string? previousId = null)
    {
        PreviousId = previousId;
        Item = item;
    }

    /// <summary>
    /// Gets or sets the optional previous conversation item ID.
    /// If not set, the new item will be appended to the end of the conversation.
    /// </summary>
    public string? PreviousId { get; set; }

    /// <summary>
    /// Gets or sets the conversation item to create.
    /// </summary>
    public RealtimeContentItem Item { get; set; }
}

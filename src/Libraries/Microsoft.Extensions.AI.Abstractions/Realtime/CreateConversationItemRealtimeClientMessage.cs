// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for creating a conversation item.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class CreateConversationItemRealtimeClientMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateConversationItemRealtimeClientMessage"/> class.
    /// </summary>
    /// <param name="item">The conversation item to create.</param>
    public CreateConversationItemRealtimeClientMessage(RealtimeConversationItem item)
    {
        Item = Throw.IfNull(item);
    }

    /// <summary>
    /// Gets or sets the conversation item to create.
    /// </summary>
    public RealtimeConversationItem Item { get; set; }
}

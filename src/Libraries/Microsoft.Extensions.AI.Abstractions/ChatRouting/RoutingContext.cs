// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides request-specific inputs to a <see cref="RoutingChatClient"/>.</summary>
/// <remarks>
/// <para>
/// One context is created for each call to <see cref="IChatClient.GetResponseAsync"/> and for each enumeration
/// started from the sequence returned by <see cref="IChatClient.GetStreamingResponseAsync"/>.
/// </para>
/// <para>
/// <see cref="ChatOptions"/> is cloned when the context is created. It is provided for client selection and is
/// independent of both the caller's instance and the options passed to the selected client.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRoutingChat, UrlFormat = DiagnosticIds.UrlFormat)]
public class RoutingContext
{
    /// <summary>Initializes a new instance of the <see cref="RoutingContext"/> class.</summary>
    /// <param name="messages">The messages to route.</param>
    /// <param name="chatOptions">The options associated with this context.</param>
    public RoutingContext(
        IEnumerable<ChatMessage> messages,
        ChatOptions? chatOptions)
    {
        _ = Throw.IfNull(messages);

        Messages = messages;
        ChatOptions = chatOptions?.Clone();
    }

    /// <summary>Gets the messages supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// Selection and failover may enumerate this sequence multiple times. Callers should supply a repeatable sequence.
    /// </remarks>
    public IEnumerable<ChatMessage> Messages { get; }

    /// <summary>Gets a snapshot of the request options supplied to client selection.</summary>
    /// <remarks>
    /// Changes do not affect the caller's instance or the options passed to the selected client. Client-specific
    /// behavior should generally be attached to the returned client.
    /// </remarks>
    public ChatOptions? ChatOptions { get; }
}

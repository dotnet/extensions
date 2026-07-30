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
/// <see cref="ChatOptions"/> is cloned when the context is created, so request-specific changes do not mutate the
/// caller's instance.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRoutingChat, UrlFormat = DiagnosticIds.UrlFormat)]
public class RoutingContext
{
    /// <summary>Initializes a new instance of the <see cref="RoutingContext"/> class.</summary>
    /// <param name="messages">The messages to route.</param>
    /// <param name="chatOptions">The options to clone for this request, or <see langword="null"/>.</param>
    public RoutingContext(
        IEnumerable<ChatMessage> messages,
        ChatOptions? chatOptions)
    {
        Messages = Throw.IfNull(messages);
        ChatOptions = chatOptions?.Clone();
    }

    /// <summary>Gets the messages supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// Selection and failover may enumerate this sequence multiple times. Callers should supply a repeatable sequence.
    /// </remarks>
    public IEnumerable<ChatMessage> Messages { get; }

    /// <summary>Gets the request-local options supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// When the caller supplies options, this is a clone that selectors may adjust without mutating the caller's
    /// instance. Changes are observed by the selected client and subsequent failover attempts. Stable route-specific
    /// defaults should generally be attached to the returned client rather than reapplied during selection.
    /// </remarks>
    public ChatOptions? ChatOptions { get; }

}

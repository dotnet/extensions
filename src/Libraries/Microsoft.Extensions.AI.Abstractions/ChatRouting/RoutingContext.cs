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
/// A derived client may replace <see cref="Messages"/> or <see cref="ChatOptions"/> during selection. The selected
/// client receives the updated values.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRoutingChat, UrlFormat = DiagnosticIds.UrlFormat)]
public class RoutingContext
{
    /// <summary>Initializes a new instance of the <see cref="RoutingContext"/> class.</summary>
    /// <param name="messages">The messages to route.</param>
    /// <param name="chatOptions">The options supplied for the request.</param>
    public RoutingContext(
        IEnumerable<ChatMessage> messages,
        ChatOptions? chatOptions)
    {
        Messages = Throw.IfNull(messages);
        ChatOptions = chatOptions;
    }

    /// <summary>Gets or sets the messages supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// The sequence may be enumerated during selection and again by one or more selected clients. Callers should
    /// supply a repeatable sequence, or a selector may replace it with a materialized sequence when required.
    /// </remarks>
    public IEnumerable<ChatMessage> Messages
    {
        get;
        set => field = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the options supplied to client selection and the selected client.</summary>
    public ChatOptions? ChatOptions { get; set; }
}

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
/// Selectors should generally treat the request inputs as read-only and return a client already configured for
/// route-specific behavior. <see cref="BufferMessages"/> provides explicit request-local buffering when repeatable
/// message enumeration is required.
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

    /// <summary>Gets the messages supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// Selectors should generally treat this sequence as input. A selector that must enumerate the sequence should use
    /// <see cref="BufferMessages"/> when repeatable enumeration is required.
    /// </remarks>
    public IEnumerable<ChatMessage> Messages { get; private set; }

    /// <summary>Gets the options supplied to client selection and the selected client.</summary>
    /// <remarks>
    /// Selectors should generally treat this instance as input and return a client already configured for
    /// route-specific behavior. Because <see cref="ChatOptions"/> is mutable, changes to the instance are observed by
    /// the selected client and subsequent failover attempts.
    /// </remarks>
    public ChatOptions? ChatOptions { get; }

    /// <summary>Returns the messages as a repeatable list, buffering the sequence when necessary.</summary>
    /// <returns>The existing message list, or a list created and cached by enumerating <see cref="Messages"/> once.</returns>
    /// <remarks>
    /// The cached list is used by subsequent selectors and selected clients for this request. Existing
    /// <see cref="IReadOnlyList{T}"/> instances and individual messages are not cloned.
    /// </remarks>
    public IReadOnlyList<ChatMessage> BufferMessages()
    {
        if (Messages is not IReadOnlyList<ChatMessage> buffered)
        {
            buffered = [.. Messages];
            Messages = buffered;
        }

        return buffered;
    }
}

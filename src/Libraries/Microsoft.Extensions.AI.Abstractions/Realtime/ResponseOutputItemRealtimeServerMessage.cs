// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message representing a new output item added or created during response generation.
/// </summary>
/// <remarks>
/// <para>
/// Used with the <see cref="RealtimeServerMessageType.ResponseOutputItemDone"/> and <see cref="RealtimeServerMessageType.ResponseOutputItemAdded"/> messages.
/// </para>
/// <para>
/// Provider implementations should emit this message with <see cref="RealtimeServerMessageType.ResponseOutputItemDone"/>
/// when an output item (such as a function call or text message) has completed. The built-in
/// <see langword="FunctionInvokingRealtimeClientSession"/> middleware depends on this message to detect
/// and invoke tool calls.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class ResponseOutputItemRealtimeServerMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseOutputItemRealtimeServerMessage"/> class.
    /// </summary>
    /// <remarks>
    /// The <paramref name="type"/> should be <see cref="RealtimeServerMessageType.ResponseOutputItemDone"/> or <see cref="RealtimeServerMessageType.ResponseOutputItemAdded"/>.
    /// </remarks>
    public ResponseOutputItemRealtimeServerMessage(RealtimeServerMessageType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the unique response ID.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> for providers that do not natively track response lifecycle.
    /// </remarks>
    public string? ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the unique output index.
    /// </summary>
    public int? OutputIndex { get; set; }

    /// <summary>
    /// Gets or sets the conversation item included in the response.
    /// </summary>
    public RealtimeConversationItem? Item { get; set; }
}

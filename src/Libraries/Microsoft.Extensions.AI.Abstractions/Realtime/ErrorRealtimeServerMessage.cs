// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server error message.
/// </summary>
/// <remarks>
/// Used with the <see cref="RealtimeServerMessageType.Error"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class ErrorRealtimeServerMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorRealtimeServerMessage"/> class.
    /// </summary>
    public ErrorRealtimeServerMessage()
    {
        Type = RealtimeServerMessageType.Error;
    }

    /// <summary>
    /// Gets or sets the error content associated with the error message.
    /// </summary>
    public ErrorContent? Error { get; set; }

    /// <summary>
    /// Gets or sets the ID of the client message that caused the error.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="RealtimeServerMessage.MessageId"/>, which identifies this server message itself,
    /// this property identifies the originating client message that triggered the error.
    /// </remarks>
    public string? OriginatingMessageId { get; set; }
}

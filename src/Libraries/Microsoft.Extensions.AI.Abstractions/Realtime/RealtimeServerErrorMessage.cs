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
public class RealtimeServerErrorMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerErrorMessage"/> class.
    /// </summary>
    public RealtimeServerErrorMessage()
    {
        Type = RealtimeServerMessageType.Error;
    }

    /// <summary>
    /// Gets or sets the error content associated with the error message.
    /// </summary>
    public ErrorContent? Error { get; set; }

    /// <summary>
    /// Gets or sets the message ID of the client message that caused the error.
    /// </summary>
    /// <remarks>
    /// This is specific to event-driven protocols where multiple client messages may be in-flight,
    /// allowing correlation of the error to the originating client request.
    /// </remarks>
    public string? ErrorMessageId { get; set; }

}

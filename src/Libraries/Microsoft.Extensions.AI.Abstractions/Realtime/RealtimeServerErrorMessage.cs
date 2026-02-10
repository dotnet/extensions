// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server error message.
/// </summary>
/// <remarks>
/// Used with the <see cref="RealtimeServerMessageType.Error"/>.
/// </remarks>
[Experimental("MEAI001")]
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
    /// Gets or sets an optional event ID caused the error.
    /// </summary>
    public string? ErrorEventId { get; set; }

    /// <summary>
    /// Gets or sets an optional parameter providing additional context about the error.
    /// </summary>
    public string? Parameter { get; set; }

}

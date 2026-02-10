// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server response message.
/// </summary>
[Experimental("MEAI001")]
public class RealtimeServerMessage
{
    /// <summary>
    /// Gets or sets the type of the real-time response.
    /// </summary>
    public RealtimeServerMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the optional event ID associated with the response.
    /// This can be used for tracking and correlation purposes.
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Gets or sets the raw representation of the response.
    /// This can be used to hold the original data structure received from the model.
    /// </summary>
    /// <remarks>
    /// The raw representation is typically used for custom or unsupported message types.
    /// For example, the model may accept a JSON serialized server message.
    /// </remarks>
    public object? RawRepresentation { get; set; }
}

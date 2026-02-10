// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message the client sends to the model.
/// </summary>
[Experimental("MEAI001")]
public class RealtimeClientMessage
{
    /// <summary>
    /// Gets or sets the optional event ID associated with the message.
    /// This can be used for tracking and correlation purposes.
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Gets or sets the raw representation of the message.
    /// This can be used to send the raw data to the model.
    /// </summary>
    /// <remarks>
    /// The raw representation is typically used for custom or unsupported message types.
    /// For example, the model may accept a JSON serialized message.
    /// </remarks>
    public object? RawRepresentation { get; set; }
}

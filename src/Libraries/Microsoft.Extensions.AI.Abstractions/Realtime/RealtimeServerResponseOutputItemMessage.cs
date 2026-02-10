// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message representing a new output item added or created during response generation.
/// </summary>
/// <remarks>
/// Used with the <see cref="RealtimeServerMessageType.ResponseDone"/> and <see cref="RealtimeServerMessageType.ResponseCreated"/> messages.
/// </remarks>
[Experimental("MEAI001")]
public class RealtimeServerResponseOutputItemMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerResponseOutputItemMessage"/> class.
    /// </summary>
    /// <remarks>
    /// The <paramref name="type"/> should be <see cref="RealtimeServerMessageType.ResponseDone"/> or <see cref="RealtimeServerMessageType.ResponseCreated"/>.
    /// </remarks>
    public RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the unique response ID.
    /// </summary>
    public string? ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the unique output index.
    /// </summary>
    public int? OutputIndex { get; set; }

    /// <summary>
    /// Gets or sets the conversation item included in the response.
    /// </summary>
    public RealtimeContentItem? Item { get; set; }
}

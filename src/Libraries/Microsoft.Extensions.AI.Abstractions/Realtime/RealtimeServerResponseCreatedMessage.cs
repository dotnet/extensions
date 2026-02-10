// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for creating a response item.
/// </summary>
/// <remarks>
/// Used with the <see cref="RealtimeServerMessageType.ResponseDone"/> and <see cref="RealtimeServerMessageType.ResponseCreated"/> messages.
/// </remarks>
[Experimental("MEAI001")]
public class RealtimeServerResponseCreatedMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerResponseCreatedMessage"/> class.
    /// </summary>
    /// <remarks>
    /// The <paramref name="type"/> should be <see cref="RealtimeServerMessageType.ResponseDone"/> or <see cref="RealtimeServerMessageType.ResponseCreated"/>.
    /// </remarks>
    public RealtimeServerResponseCreatedMessage(RealtimeServerMessageType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the output audio options for the response. If null, the default conversation audio options will be used.
    /// </summary>
    public RealtimeAudioFormat? OutputAudioOptions { get; set; }

    /// <summary>
    /// Gets or sets the voice of the output audio.
    /// </summary>
    public string? OutputVoice { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID associated with the response.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the unique response ID.
    /// </summary>
    public string? ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of output tokens for the response.
    /// If 0, the service will apply its own limit.
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the message.
    /// </summary>
    public AdditionalPropertiesDictionary? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the list of the conversation items included in the response.
    /// </summary>
    public IList<RealtimeContentItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the output modalities for the response. like "text", "audio".
    /// If null, then default conversation modalities will be used.
    /// </summary>
    public IList<string>? OutputModalities { get; set; }

    /// <summary>
    /// Gets or sets the status of the response.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the error content of the response, if any.
    /// </summary>
    public ErrorContent? Error { get; set; }

    /// <summary>
    /// Gets or sets the usage details for the response.
    /// </summary>
    public UsageDetails? Usage { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for creating a response item.
/// </summary>
/// <remarks>
/// Used with the <see cref="RealtimeServerMessageType.ResponseDone"/> and <see cref="RealtimeServerMessageType.ResponseCreated"/> messages.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
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
    /// <remarks>
    /// Identifies which conversation within the session this response belongs to.
    /// A session may have a default conversation to which items are automatically added,
    /// or responses may be generated out-of-band (not associated with any conversation).
    /// </remarks>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the unique response ID.
    /// </summary>
    public string? ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of output tokens for the response, inclusive of all modalities and tool calls.
    /// </summary>
    /// <remarks>
    /// This limit applies to the total output tokens regardless of modality (text, audio, etc.).
    /// If <see langword="null"/>, the provider's default limit was used.
    /// </remarks>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets any additional properties associated with the response.
    /// </summary>
    /// <remarks>
    /// Contains arbitrary key-value metadata attached to the response.
    /// This is the metadata that was provided when the response was created
    /// (e.g., for tracking or disambiguating multiple simultaneous responses).
    /// </remarks>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

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
    /// Gets or sets the per-response token usage for billing purposes.
    /// </summary>
    /// <remarks>
    /// Populated when the response is complete (i.e., on <see cref="RealtimeServerMessageType.ResponseDone"/>).
    /// Input tokens include the entire conversation context, so they grow over successive turns
    /// as previous output becomes input for later responses.
    /// </remarks>
    public UsageDetails? Usage { get; set; }
}

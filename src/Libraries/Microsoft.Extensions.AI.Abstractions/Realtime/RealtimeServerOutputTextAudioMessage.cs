// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server message for output text and audio.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class RealtimeServerOutputTextAudioMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerOutputTextAudioMessage"/> class for handling output text delta responses.
    /// </summary>
    /// <param name="type">The type of the real-time server response.</param>
    /// <remarks>
    /// The <paramref name="type"/> should be <see cref="RealtimeServerMessageType.OutputTextDelta"/>, <see cref="RealtimeServerMessageType.OutputTextDone"/>,
    /// <see cref="RealtimeServerMessageType.OutputAudioTranscriptionDelta"/>, <see cref="RealtimeServerMessageType.OutputAudioTranscriptionDone"/>,
    /// <see cref="RealtimeServerMessageType.OutputAudioDelta"/>, or <see cref="RealtimeServerMessageType.OutputAudioDone"/>.
    /// </remarks>
    public RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the index of the content part whose text has been updated.
    /// </summary>
    public int? ContentIndex { get; set; }

    /// <summary>
    /// Gets or sets the text delta or final text content.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="RealtimeServerMessageType.OutputTextDelta"/>, <see cref="RealtimeServerMessageType.OutputTextDone"/>,
    /// <see cref="RealtimeServerMessageType.OutputAudioTranscriptionDelta"/>, and <see cref="RealtimeServerMessageType.OutputAudioTranscriptionDone"/> messages.
    /// For audio messages (<see cref="RealtimeServerMessageType.OutputAudioDelta"/> and <see cref="RealtimeServerMessageType.OutputAudioDone"/>),
    /// use <see cref="Audio"/> instead.
    /// </remarks>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the Base64-encoded audio data delta or final audio content.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="RealtimeServerMessageType.OutputAudioDelta"/> messages.
    /// For <see cref="RealtimeServerMessageType.OutputAudioDone"/>, this is typically <see langword="null"/>
    /// as the final audio is not included; use the accumulated deltas instead.
    /// For text content, use <see cref="Text"/> instead.
    /// </remarks>
    public string? Audio { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item containing the content part whose text has been updated.
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the index of the output item in the response.
    /// </summary>
    public int? OutputIndex { get; set; }

    /// <summary>
    /// Gets or sets the ID of the response.
    /// </summary>
    public string? ResponseId { get; set; }
}

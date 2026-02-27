// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server message for input audio transcription.
/// </summary>
/// <remarks>
/// Used when having InputAudioTranscriptionCompleted, InputAudioTranscriptionDelta, or InputAudioTranscriptionFailed response types.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class RealtimeServerInputAudioTranscriptionMessage : RealtimeServerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerInputAudioTranscriptionMessage"/> class.
    /// </summary>
    /// <param name="type">The type of the real-time server response.</param>
    /// <remarks>
    /// The <paramref name="type"/> parameter should be InputAudioTranscriptionCompleted, InputAudioTranscriptionDelta, or InputAudioTranscriptionFailed.
    /// </remarks>
    public RealtimeServerInputAudioTranscriptionMessage(RealtimeServerMessageType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the index of the content part containing the audio.
    /// </summary>
    public int? ContentIndex { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item containing the audio that is being transcribed.
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the transcription text of the audio.
    /// </summary>
    public string? Transcription { get; set; }

    /// <summary>
    /// Gets or sets the transcription-specific usage, which is billed separately from the realtime model.
    /// </summary>
    /// <remarks>
    /// This usage reflects the cost of the speech-to-text transcription and is billed according to the
    /// ASR (Automatic Speech Recognition) model's pricing rather than the realtime model's pricing.
    /// </remarks>
    public UsageDetails? Usage { get; set; }

    /// <summary>
    /// Gets or sets the error content if an error occurred during transcription.
    /// </summary>
    public ErrorContent? Error { get; set; }
}

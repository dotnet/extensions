// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server message for input audio transcription.
/// </summary>
/// <remarks>
/// Used when having InputAudioTranscriptionCompleted, InputAudioTranscriptionDelta, or InputAudioTranscriptionFailed response types.
/// </remarks>
[Experimental("MEAI001")]
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
    /// Gets or sets the usage details for the transcription.
    /// </summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>
    /// Gets or sets the error content if an error occurred during transcription.
    /// </summary>
    public ErrorContent? Error { get; set; }
}

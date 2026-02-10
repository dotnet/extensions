// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time server message for output text and audio.
/// </summary>
[Experimental("MEAI001")]
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
    /// Gets or sets the text or audio delta, or the final text or audio once the output is complete.
    /// </summary>
    /// <remarks>
    /// if dealing with audio content, this property may contain Base64-encoded audio data.
    /// With <see cref="RealtimeServerMessageType.OutputAudioDone"/>, usually will have null Text value.
    /// </remarks>
    public string? Text { get; set; }

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

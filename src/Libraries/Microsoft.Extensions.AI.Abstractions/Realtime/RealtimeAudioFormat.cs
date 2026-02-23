// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring real-time audio.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class RealtimeAudioFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeAudioFormat"/> class.
    /// </summary>
    public RealtimeAudioFormat(string mediaType, int sampleRate)
    {
        MediaType = mediaType;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// Gets or sets the media type of the audio (e.g., "audio/pcm", "audio/pcmu", "audio/pcma").
    /// </summary>
    public string MediaType { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the audio in Hertz.
    /// </summary>
    /// <remarks>
    /// When constructed via <see cref="RealtimeAudioFormat(string, int)"/>, this property is always set.
    /// The nullable type allows deserialized instances to omit the sample rate when the server does not provide one.
    /// </remarks>
    public int? SampleRate { get; set; }
}

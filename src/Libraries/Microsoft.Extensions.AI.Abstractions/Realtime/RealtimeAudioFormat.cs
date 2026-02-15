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
    public RealtimeAudioFormat(string type, int sampleRate)
    {
        Type = type;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// Gets or sets the type of audio. For example, "audio/pcm".
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the audio in Hertz.
    /// </summary>
    /// <remarks>
    /// When constructed via <see cref="RealtimeAudioFormat(string, int)"/>, this property is always set.
    /// The nullable type allows deserialized instances to omit the sample rate when the server does not provide one.
    /// </remarks>
    public int? SampleRate { get; set; }
}

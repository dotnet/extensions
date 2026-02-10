// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring server voice activity detection in a real-time session.
/// </summary>
[Experimental("MEAI001")]
public class ServerVoiceActivityDetection : VoiceActivityDetection
{
    /// <summary>
    /// Gets or sets the idle timeout in milliseconds to detect the end of speech.
    /// </summary>
    public int IdleTimeoutInMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the prefix padding in milliseconds to include before detected speech.
    /// </summary>
    public int PrefixPaddingInMilliseconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the silence duration in milliseconds to consider as a pause.
    /// </summary>
    public int SilenceDurationInMilliseconds { get; set; } = 500;

    /// <summary>
    /// Gets or sets the threshold for voice activity detection.
    /// </summary>
    /// <remarks>
    /// A value between 0.0 and 1.0, where higher values make the detection more sensitive.
    /// </remarks>
    public double Threshold { get; set; } = 0.5;
}

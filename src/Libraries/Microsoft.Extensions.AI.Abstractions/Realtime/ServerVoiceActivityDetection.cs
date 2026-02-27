// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring server voice activity detection in a real-time session.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class ServerVoiceActivityDetection : VoiceActivityDetection
{
    /// <summary>
    /// Gets the idle timeout in milliseconds to detect the end of speech.
    /// </summary>
    public int IdleTimeoutInMilliseconds { get; init; }

    /// <summary>
    /// Gets the prefix padding in milliseconds to include before detected speech.
    /// </summary>
    public int PrefixPaddingInMilliseconds { get; init; } = 300;

    /// <summary>
    /// Gets the silence duration in milliseconds to consider as a pause.
    /// </summary>
    public int SilenceDurationInMilliseconds { get; init; } = 500;

    /// <summary>
    /// Gets the threshold for voice activity detection.
    /// </summary>
    /// <remarks>
    /// A value between 0.0 and 1.0, where higher values make the detection more sensitive.
    /// </remarks>
    public double Threshold { get; init; } = 0.5;
}

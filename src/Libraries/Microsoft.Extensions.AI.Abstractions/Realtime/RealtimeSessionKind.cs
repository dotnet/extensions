// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring a real-time session.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public enum RealtimeSessionKind
{
    /// <summary>
    /// Represent a realtime sessions which process audio, text, or other media in real-time.
    /// </summary>
    Realtime,

    /// <summary>
    /// Represent transcription only session.
    /// </summary>
    Transcription
}

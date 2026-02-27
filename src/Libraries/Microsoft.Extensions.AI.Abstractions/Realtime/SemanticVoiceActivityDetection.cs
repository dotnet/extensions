// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring server voice activity detection in a real-time session.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class SemanticVoiceActivityDetection : VoiceActivityDetection
{
    /// <summary>
    /// Gets the eagerness level for semantic voice activity detection.
    /// </summary>
    public SemanticEagerness Eagerness { get; init; } = SemanticEagerness.Auto;
}

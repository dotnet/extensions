// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring server voice activity detection in a real-time session.
/// </summary>
[Experimental("MEAI001")]
public class SemanticVoiceActivityDetection : VoiceActivityDetection
{
    /// <summary>
    /// Gets or sets the eagerness level for semantic voice activity detection.
    /// </summary>
    /// <remarks>
    /// Examples of the values are "low", "medium", "high", and "auto".
    /// </remarks>
    public string Eagerness { get; set; } = "auto";
}

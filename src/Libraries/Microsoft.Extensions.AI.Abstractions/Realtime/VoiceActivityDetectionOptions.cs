// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring voice activity detection (VAD) in a real-time session.
/// </summary>
/// <remarks>
/// Voice activity detection automatically determines when a user starts and stops speaking,
/// enabling natural turn-taking in conversational audio interactions.
/// When <see cref="Enabled"/> is <see langword="true"/>, the server detects speech boundaries
/// and manages turn transitions automatically.
/// When <see cref="Enabled"/> is <see langword="false"/>, the client must explicitly signal
/// activity boundaries (e.g., via audio buffer commit and response creation).
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class VoiceActivityDetectionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceActivityDetectionOptions"/> class.
    /// </summary>
    public VoiceActivityDetectionOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether server-side voice activity detection is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the server automatically detects speech start and end,
    /// and may automatically trigger responses when the user stops speaking.
    /// When <see langword="false"/>, the client controls turn boundaries manually.
    /// The default is <see langword="true"/>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the user's speech can interrupt the model's audio output.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the model's response will be cut off when the user starts speaking (barge-in).
    /// When <see langword="false"/>, the model's response will continue to completion regardless of user input.
    /// The default is <see langword="true"/>.
    /// Not all providers support this option; those that do not will ignore it.
    /// </remarks>
    public bool AllowInterruption { get; set; } = true;
}

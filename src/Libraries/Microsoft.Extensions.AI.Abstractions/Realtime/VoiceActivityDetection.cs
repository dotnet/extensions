// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring voice activity detection in a real-time session.
/// </summary>
[Experimental("MEAI001")]
public class VoiceActivityDetection
{
    /// <summary>
    /// Gets or sets a value indicating whether to create a response when voice activity is detected.
    /// </summary>
    public bool CreateResponse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to interrupt the response when voice activity is detected.
    /// </summary>
    public bool InterruptResponse { get; set; }
}

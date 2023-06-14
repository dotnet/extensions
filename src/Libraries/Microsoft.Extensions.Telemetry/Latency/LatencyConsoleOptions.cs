// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry;

/// <summary>
/// Options for console latency data exporter.
/// </summary>
public class LatencyConsoleOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to emit latency checkpoint information to the console.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool OutputCheckpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency tag information to the console.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool OutputTags { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency measure information to the console.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool OutputMeasures { get; set; } = true;
}

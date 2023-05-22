// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Console;

/// <summary>
/// Options for console latency data exporter.
/// </summary>
public class LarencyConsoleOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to emit latency checkpoint information to the console.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true" />.
    /// </remarks>
    public bool OutputCheckpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency tag information to the console.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true" />.
    /// </remarks>
    public bool OutputTags { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency measure information to the console.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true" />.
    /// </remarks>
    public bool OutputMeasures { get; set; } = true;
}

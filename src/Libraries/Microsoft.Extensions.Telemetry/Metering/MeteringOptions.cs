// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Options for configuring metering.
/// </summary>
[Experimental]
public class MeteringOptions
{
    /// <summary>
    /// Gets or sets maximum number of Metric streams supported.
    /// </summary>
    /// <remarks>
    /// Default value is set to 1000.
    /// </remarks>
    public int MaxMetricStreams { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of metric points allowed per metric stream.
    /// </summary>
    /// <remarks>
    /// Default value is set to 2000.
    /// </remarks>
    public int MaxMetricPointsPerStream { get; set; } = 2000;

    /// <summary>
    /// Gets or sets default meter state to be used for emitting metrics.
    /// </summary>
    /// <value>
    /// The default value is <see cref="MeteringState.Enabled"/>.
    /// </value>
    public MeteringState MeterState { get; set; } = MeteringState.Enabled;

    /// <summary>
    /// Gets or sets metering state override to control metering state for specific categories.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///    "MeterState": {
    ///       "Microsoft.Extensions.Cache.Meter": "Disabled"
    ///    }
    /// }
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Set for options validation")]
    public IDictionary<string, MeteringState> MeterStateOverrides { get; set; } =
        new Dictionary<string, MeteringState>();
}

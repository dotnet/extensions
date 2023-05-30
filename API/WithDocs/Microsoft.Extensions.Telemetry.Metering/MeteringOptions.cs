// Assembly 'Microsoft.Extensions.Telemetry'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Options for configuring metering.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class MeteringOptions
{
    /// <summary>
    /// Gets or sets maximum number of Metric streams supported.
    /// </summary>
    /// <remarks>
    /// Default value is set to 1000.
    /// </remarks>
    public int MaxMetricStreams { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of metric points allowed per metric stream.
    /// </summary>
    /// <remarks>
    /// Default value is set to 2000.
    /// </remarks>
    public int MaxMetricPointsPerStream { get; set; }

    /// <summary>
    /// Gets or sets default meter state to be used for emitting metrics.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Telemetry.Metering.MeteringState.Enabled" />.
    /// </value>
    public MeteringState MeterState { get; set; }

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
    public IDictionary<string, MeteringState> MeterStateOverrides { get; set; }

    public MeteringOptions();
}

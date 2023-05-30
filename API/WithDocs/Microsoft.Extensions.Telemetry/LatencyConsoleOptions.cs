// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;

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
    public bool OutputCheckpoints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency tag information to the console.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool OutputTags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to emit latency measure information to the console.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool OutputMeasures { get; set; }

    public LatencyConsoleOptions();
}

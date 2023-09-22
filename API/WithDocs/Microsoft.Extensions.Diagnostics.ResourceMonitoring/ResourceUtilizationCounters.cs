// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Represents the names of instruments published by this package.
/// </summary>
/// <remarks>
/// These counters are currently only published on Linux.
/// </remarks>
/// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />
public static class ResourceUtilizationCounters
{
    /// <summary>
    /// Gets the CPU consumption of the running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" />.
    /// </remarks>
    public static string CpuConsumptionPercentage { get; }

    /// <summary>
    /// Gets the memory consumption of the running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" />.
    /// </remarks>
    public static string MemoryConsumptionPercentage { get; }
}

// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Represents the names of instruments published by this package.
/// </summary>
/// <seealso cref="T:System.Diagnostics.Metrics.Instrument" />
public static class LinuxResourceUtilizationCounters
{
    /// <summary>
    /// Gets the CPU consumption of the running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" /> (long).
    /// </remarks>
    public static string CpuConsumptionPercentage { get; }

    /// <summary>
    /// Gets the memory consumption of the running application in percentages.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="T:System.Diagnostics.Metrics.ObservableGauge`1" /> (long).
    /// </remarks>
    public static string MemoryConsumptionPercentage { get; }
}

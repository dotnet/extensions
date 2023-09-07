// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Captures resource usage at a given point in time.
/// </summary>
public readonly struct Utilization
{
    /// <summary>
    /// Gets the CPU utilization percentage.
    /// </summary>
    /// <remarks>
    /// This percentage is calculated relative to the <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.SystemResources.GuaranteedCpuUnits" />.
    /// </remarks>
    public double CpuUsedPercentage { get; }

    /// <summary>
    /// Gets the memory utilization percentage.
    /// </summary>
    /// <remarks>
    /// This percentage is calculated relative to the <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.SystemResources.GuaranteedMemoryInBytes" />.
    /// This is an instantaneous measure when the utilization was computed, NOT an average.
    /// </remarks>
    public double MemoryUsedPercentage { get; }

    /// <summary>
    /// Gets the total memory used.
    /// </summary>
    /// <remarks>
    /// This is an instantaneous measure when the utilization was computed, NOT an average.
    /// </remarks>
    public ulong MemoryUsedInBytes { get; }

    /// <summary>
    /// Gets the CPU and memory limits defined by the underlying system.
    /// </summary>
    public SystemResources SystemResources { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.Utilization" /> struct.
    /// </summary>
    /// <param name="cpuUsedPercentage">CPU utilization.</param>
    /// <param name="memoryUsedInBytes">Memory used in bytes (instantaneous).</param>
    /// <param name="systemResources">CPU and memory limits.</param>
    public Utilization(double cpuUsedPercentage, ulong memoryUsedInBytes, SystemResources systemResources);
}

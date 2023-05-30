// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// CPU and memory limits defined by the underlying system.
/// </summary>
public readonly struct SystemResources
{
    /// <summary>
    /// Gets the CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the guaranteed CPUs as described by Kubernetes CPU request parameter, each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the POD is configured with 1500m units as the CPU request, this property will be assigned
    /// to 1.5 which means one and a half CPU will be dedicated for the POD.
    /// </remarks>
    public double GuaranteedCpuUnits { get; }

    /// <summary>
    /// Gets the maximum CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the maximum CPUs as described by Kubernetes CPU limit parameter, each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the is configured with 1500m units as the CPU limit, this property will be assigned
    /// to 1.5 which means one and a half CPU will be the maximum CPU available for the POD.
    /// </remarks>
    public double MaximumCpuUnits { get; }

    /// <summary>
    /// Gets the memory allocated to the system in bytes.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the guaranteed memory as configured by a Kubernetes memory request parameter.
    /// </remarks>
    public ulong GuaranteedMemoryInBytes { get; }

    /// <summary>
    /// Gets the maximum memory allocated to the system in bytes.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the maximum memory as configured by a Kubernetes memory limit parameter.
    /// </remarks>
    public ulong MaximumMemoryInBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.SystemResources" /> struct.
    /// </summary>
    /// <param name="guaranteedCpuUnits">The CPU units available in the system.</param>
    /// <param name="maximumCpuUnits">The maximum CPU units available in the system.</param>
    /// <param name="guaranteedMemoryInBytes">The memory allocated to the system in bytes.</param>
    /// <param name="maximumMemoryInBytes">The maximum memory allocated to the system in bytes.</param>
    public SystemResources(double guaranteedCpuUnits, double maximumCpuUnits, ulong guaranteedMemoryInBytes, ulong maximumMemoryInBytes);
}

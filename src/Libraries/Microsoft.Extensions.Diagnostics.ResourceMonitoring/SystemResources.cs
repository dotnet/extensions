using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// CPU and memory limits defined by the underlying system.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct SystemResources
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    /// <summary>
    /// Gets the CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the guaranteed CPUs as described by Kubernetes CPU request parameter, each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the POD is configured with 1500m units as the CPU request, this property will be assigned
    /// to 1.5 which means one and a half CPU will be dedicated for the POD.
    /// For POD this value is calculated based on the cgroupv2 weight, using the formula.
    /// y = (1 + ((x - 2) * 9999) / 262142), where y is the CPU weight and x is the CPU share (cgroup v1).
    /// https://github.com/kubernetes/enhancements/tree/master/keps/sig-node/2254-cgroup-v2#phase-1-convert-from-cgroups-v1-settings-to-v2.
    /// </remarks>
    public double GuaranteedCpuUnits { get; }

    /// <summary>
    /// Gets the maximum CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the maximum CPUs as described by Kubernetes CPU limit parameter, each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the is configured with 1500m units as the CPU limit, this property will be assigned
    /// to 1.5 which means one and a half CPU will be the maximum CPU available.
    /// </remarks>
    public double MaximumCpuUnits { get; }

    /// <summary>
    /// Gets the memory allocated to the system in bytes.
    /// </summary>
    public ulong GuaranteedMemoryInBytes { get; }

    /// <summary>
    /// Gets the Container's Request Memory Limit or the Maximum allocated for the VM.
    /// </summary>
    public ulong MaximumMemoryInBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemResources"/> struct.
    /// </summary>
    /// <param name="guaranteedCpuUnits">The CPU units available in the system.</param>
    /// <param name="maximumCpuUnits">The maximum CPU units available in the system.</param>
    /// <param name="guaranteedMemoryInBytes">The memory allocated to the system in bytes.</param>
    /// <param name="maximumMemoryInBytes">The maximum memory allocated to the system in bytes.</param>
    public SystemResources(double guaranteedCpuUnits, double maximumCpuUnits, ulong guaranteedMemoryInBytes, ulong maximumMemoryInBytes)
    {
        GuaranteedCpuUnits = Throw.IfLessThanOrEqual(guaranteedCpuUnits, 0.0);
        MaximumCpuUnits = Throw.IfLessThanOrEqual(maximumCpuUnits, 0.0);
        GuaranteedMemoryInBytes = Throw.IfLessThan(guaranteedMemoryInBytes, 1UL);
        MaximumMemoryInBytes = Throw.IfLessThan(maximumMemoryInBytes, 1UL);
    }
}

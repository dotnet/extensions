// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public readonly struct SystemResources
{
    public double GuaranteedCpuUnits { get; }
    public double MaximumCpuUnits { get; }
    public ulong GuaranteedMemoryInBytes { get; }
    public ulong MaximumMemoryInBytes { get; }
    public SystemResources(double guaranteedCpuUnits, double maximumCpuUnits, ulong guaranteedMemoryInBytes, ulong maximumMemoryInBytes);
}

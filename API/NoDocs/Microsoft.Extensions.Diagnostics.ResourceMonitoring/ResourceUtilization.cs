// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public readonly struct ResourceUtilization
{
    public double CpuUsedPercentage { get; }
    public double MemoryUsedPercentage { get; }
    public ulong MemoryUsedInBytes { get; }
    public SystemResources SystemResources { get; }
    public ResourceUtilization(double cpuUsedPercentage, ulong memoryUsedInBytes, SystemResources systemResources);
}

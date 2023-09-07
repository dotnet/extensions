// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public readonly struct Utilization
{
    public double CpuUsedPercentage { get; }
    public double MemoryUsedPercentage { get; }
    public ulong MemoryUsedInBytes { get; }
    public SystemResources SystemResources { get; }
    public Utilization(double cpuUsedPercentage, ulong memoryUsedInBytes, SystemResources systemResources);
}

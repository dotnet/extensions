// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal class KubernetesResourceQuotas
{
    public double CpuLimit { get; set; }
    public double CpuRequest { get; set; }
    public ulong MemoryLimit { get; set; }
    public ulong MemoryRequest { get; set; }

    public KubernetesResourceQuotas(double cpuLimit, double cpuRequest, ulong memoryLimit, ulong memoryRequest)
    {
        CpuLimit = cpuLimit;
        CpuRequest = cpuRequest;
        MemoryLimit = memoryLimit;
        MemoryRequest = memoryRequest;
    }
}

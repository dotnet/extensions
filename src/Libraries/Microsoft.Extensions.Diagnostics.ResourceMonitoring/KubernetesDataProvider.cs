// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.ClusterMetadata.Kubernetes;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

/// <summary>
/// Provides resource data from Kubernetes metadata for Windows containers.
/// </summary>
internal sealed class KubernetesResourceDataProvider : IWindowsResourceDataProvider
{
    private readonly KubernetesClusterMetadata _kubernetesMetadata;

    public KubernetesResourceDataProvider(KubernetesClusterMetadata kubernetesMetadata)
    {
        _kubernetesMetadata = kubernetesMetadata;
    }

    public long GetCurrentCpuTicks()
    {
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.Ticks;
    }

    public ulong GetCurrentMemoryUsage()
    {
        return (ulong)Environment.WorkingSet;
    }

    public (long userTime, long kernelTime) GetCpuTimeBreakdown()
    {
        using var process = Process.GetCurrentProcess();
        return (process.UserProcessorTime.Ticks, process.PrivilegedProcessorTime.Ticks);
    }

    public WindowsResourceLimits GetResourceLimits()
    {
        double cpuRequest = ConvertMillicoreToUnit(_kubernetesMetadata.RequestsCpu);
        double cpuLimit = ConvertMillicoreToUnit(_kubernetesMetadata.LimitsCpu);
        ulong memoryRequest = _kubernetesMetadata.RequestsMemory;
        ulong memoryLimit = _kubernetesMetadata.LimitsMemory;

        return new WindowsResourceLimits(
            CpuLimit: cpuLimit,
            CpuRequest: cpuRequest,
            MemoryLimit: memoryLimit,
            MemoryRequest: memoryRequest,
            HasRequests: true);
    }

    private static double ConvertMillicoreToUnit(ulong millicores)
    {
        return millicores / 1000.0;
    }
}

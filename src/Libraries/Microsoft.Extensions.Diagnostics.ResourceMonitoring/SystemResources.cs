// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Provides information about the CPU and memory limits defined by the underlying system.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
[Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
    DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
    UrlFormat = DiagnosticIds.UrlFormat)]
public readonly struct SystemResources
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    /// <summary>
    /// Gets the CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the guaranteed CPUs as described by Kubernetes CPU request parameter. Each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the Pod is configured with 1500m units as the CPU request, this property will be assigned
    /// to 1.5, which means one and a half CPU will be dedicated for the Pod.
    /// For a Pod, this value is calculated based on the <c>cgroupv2</c> weight, using the formula
    /// <c>y = (1 + ((x - 2) * 9999) / 262142)</c>, where <c>y</c> is the CPU weight and <c>x</c> is the CPU share (<c>cgroup v1</c>).
    /// For more information, see <see href="https://github.com/kubernetes/enhancements/tree/master/keps/sig-node/2254-cgroup-v2#phase-1-convert-from-cgroups-v1-settings-to-v2"/>.
    /// </remarks>
    public double GuaranteedCpuUnits { get; }

    /// <summary>
    /// Gets the maximum CPU units available in the system.
    /// </summary>
    /// <remarks>
    /// This value corresponds to the number of the maximum CPUs as described by Kubernetes CPU limit parameter. Each 1000 CPU units
    /// represent 1 CPU or 1 Core. For example, if the Pod is configured with 1500m units as the CPU limit, this property will be assigned
    /// to 1.5, which means one and a half CPU will be the maximum CPU available.
    /// </remarks>
    public double MaximumCpuUnits { get; }

    /// <summary>
    /// Gets the memory allocated to the system in bytes.
    /// </summary>
    public ulong GuaranteedMemoryInBytes { get; }

    /// <summary>
    /// Gets the container's request memory limit or the maximum allocated for the VM.
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

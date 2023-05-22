// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Captures resource usage at a given point in time.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
public readonly struct Utilization
{
    private const double Hundred = 100.0;

    /// <summary>
    /// Gets the CPU utilization percentage.
    /// </summary>
    /// <remarks>
    /// This percentage is calculated relative to the <see cref="SystemResources.GuaranteedCpuUnits"/>.
    /// </remarks>
    public double CpuUsedPercentage { get; }

    /// <summary>
    /// Gets the memory utilization percentage.
    /// </summary>
    /// <remarks>
    /// This percentage is calculated relative to the <see cref="SystemResources.GuaranteedMemoryInBytes"/>.
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
    /// Gets the latest snapshot of resource utilization.
    /// </summary>
    internal ResourceUtilizationSnapshot Snapshot { get; } = new(new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="Utilization"/> struct.
    /// </summary>
    /// <param name="cpuUsedPercentage">CPU utilization.</param>
    /// <param name="memoryUsedInBytes">Memory used in bytes (instantaneous).</param>
    /// <param name="systemResources">CPU and memory limits.</param>
    public Utilization(double cpuUsedPercentage, ulong memoryUsedInBytes, SystemResources systemResources)
    {
        CpuUsedPercentage = Throw.IfLessThan(cpuUsedPercentage, 0.0);
        MemoryUsedInBytes = Throw.IfLessThan(memoryUsedInBytes, 0);
        SystemResources = systemResources;
        MemoryUsedPercentage = Math.Min(Hundred, (double)MemoryUsedInBytes / SystemResources.GuaranteedMemoryInBytes * Hundred);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utilization"/> struct.
    /// </summary>
    /// <param name="cpuUsedPercentage">CPU utilization.</param>
    /// <param name="memoryUsedInBytes">Memory used in bytes (instantaneous).</param>
    /// <param name="systemResources">CPU and memory limits.</param>
    /// <param name="snapShot">Latest ResourceUtilizationSnapshot.</param>
    internal Utilization(double cpuUsedPercentage, ulong memoryUsedInBytes, SystemResources systemResources, ResourceUtilizationSnapshot snapShot)
        : this(cpuUsedPercentage, memoryUsedInBytes, systemResources)
    {
        Snapshot = snapShot;
    }
}

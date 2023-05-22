// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// A Utilization Calculator.
/// </summary>
internal static class Calculator
{
    private const double Hundred = 100.0;

    /// <summary>
    /// Calculate utilization based on two successive samples.
    /// </summary>
    /// <param name="first">Sample one.</param>
    /// <param name="second">Sample two.</param>
    /// <param name="systemResources">CPU and memory limits of the system.</param>
    /// <returns>A utilization score.</returns>
    public static Utilization CalculateUtilization(in ResourceUtilizationSnapshot first, in ResourceUtilizationSnapshot second, in SystemResources systemResources)
    {
        // Compute the length of the interval between samples.
        long runtimeTickDelta = second.TotalTimeSinceStart.Ticks - first.TotalTimeSinceStart.Ticks;

        // Compute the total number of ticks available on the machine during that interval
        double totalSystemTicks = runtimeTickDelta * systemResources.GuaranteedCpuUnits;

        // Now, compute the amount of usage between the intervals
        long oldUsageTicks = first.KernelTimeSinceStart.Ticks + first.UserTimeSinceStart.Ticks;
        long newUsageTicks = second.KernelTimeSinceStart.Ticks + second.UserTimeSinceStart.Ticks;
        long totalUsageTickDelta = newUsageTicks - oldUsageTicks;

        var utilization = Math.Max(0.0, (totalUsageTickDelta / totalSystemTicks) * Hundred);
        var cpuUtilization = Math.Min(Hundred, utilization);

        return new Utilization(cpuUtilization, second.MemoryUsageInBytes, systemResources, second);
    }
}

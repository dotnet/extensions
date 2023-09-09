// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// The Perf Counter publisher.
/// </summary>
[ExcludeFromCodeCoverage]
#pragma warning disable IDE0079 // Remove unnecessary suppression
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Indeed, this whole class is for Windows only")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
internal sealed class WindowsPerfCounterPublisher : IResourceUtilizationPublisher
{
    // Since everything is represented in PerfMon as longs, rather than doubles internally, represent everything as
    // a fraction of N / 10,000.
    // When displaying, PerfMon will auto-adjust this to K / 100, and thus represent places to two decimal places.
    private const double MaximumPercentageValue = 100.0;
    private const double ScaleToRepresentTwoDecimalPlaces = 100.0;
    private const long MaximumIntervalValue = (long)(MaximumPercentageValue * ScaleToRepresentTwoDecimalPlaces);

    private readonly WindowsPerfCounters _counters;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsPerfCounterPublisher"/> class.
    /// </summary>
    /// <remarks>The constructor mainly checks for the existence of windows performance counters category and logs an error if it does not exist.</remarks>
    public WindowsPerfCounterPublisher(ILogger<WindowsPerfCounterPublisher> logger)
    {
        if (!PerformanceCounterCategory.Exists(WindowsPerfCounterConstants.ContainerCounterCategoryName))
        {
            Log.CounterDoesNotExist(logger, WindowsPerfCounterConstants.ContainerCounterCategoryName);
        }

        _counters = CreateInstanceCounter();
    }

    /// <inheritdoc/>
    public ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken)
    {
        // Throw exception if cancellation was requested.
        cancellationToken.ThrowIfCancellationRequested();

        _counters.CpuUtilization.RawValue = (long)((utilization.Snapshot.UserTimeSinceStart.Ticks + utilization.Snapshot.KernelTimeSinceStart.Ticks) / utilization.SystemResources.GuaranteedCpuUnits);
        _counters.MemUtilization.RawValue = (long)(utilization.MemoryUsedPercentage * ScaleToRepresentTwoDecimalPlaces);
        _counters.MemLimit.RawValue = MaximumIntervalValue;
        return default;
    }

    private static WindowsPerfCounters CreateInstanceCounter()
    {
        const string InstanceName = "Total";
        return new WindowsPerfCounters(
            cpuUtilization: new PerformanceCounter(WindowsPerfCounterConstants.ContainerCounterCategoryName, WindowsPerfCounterConstants.CpuUtilizationCounterName, InstanceName, false),
            memUtilization: new PerformanceCounter(WindowsPerfCounterConstants.ContainerCounterCategoryName, WindowsPerfCounterConstants.MemoryUtilizationCounterName, InstanceName, false),
            memLimit: new PerformanceCounter(WindowsPerfCounterConstants.ContainerCounterCategoryName, WindowsPerfCounterConstants.MemoryLimitCounterName, InstanceName, false));
    }
}

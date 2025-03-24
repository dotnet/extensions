// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDiskPerSecondPerfCounters
{
    private readonly List<IPerformanceCounter> _counters = [];
    private readonly IPerformanceCounterFactory _performanceCounterFactory;
    private readonly PerformanceCounterCategory _category;
    private readonly string _counterName;
    private long _lastTimestamp;

    internal WindowsDiskPerSecondPerfCounters(IPerformanceCounterFactory performanceCounterFactory, PerformanceCounterCategory category, string counterName)
    {
        _performanceCounterFactory = performanceCounterFactory;
        _category = category;
        _counterName = counterName;
    }

    /// <summary>
    /// Gets the disk I/O measurements.
    /// Key: Disk name, Value: Total count.
    /// </summary>
    internal Dictionary<string, long> TotalCountDict { get; } = [];

    internal void InitializeDiskCounters()
    {
        string[]? instanceNames = _category.GetInstanceNames();
        foreach (string instance in instanceNames)
        {
            // Skip the total instance
            if (instance.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Create counters for each disk
            _counters.Add(_performanceCounterFactory.Create(_category.CategoryName, _counterName, instance));
            TotalCountDict.Add(instance, 0);
        }

        // Initialize the counters to get the first value
        foreach (IPerformanceCounter counter in _counters)
        {
            _ = counter.NextValue();
        }

#pragma warning disable EA0002
        _lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
#pragma warning restore EA0002
    }

    internal void UpdateDiskCounters()
    {
#pragma warning disable EA0002
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
#pragma warning restore EA0002
        double elapsedTime = (currentTimestamp - _lastTimestamp) / 1000.0; // Convert to seconds

        // For the kind of "per-second" perf counters, this algorithm calculates the total value over a time interval
        // by multiplying the per-second rate (e.g., Disk Bytes/sec) by the time interval between two samples.
        // This effectively reverses the per-second rate calculation to a total amount (e.g., total bytes transferred) during that period.
        foreach (IPerformanceCounter counter in _counters)
        {
            // total value = per-second rate * elapsed time
            double value = counter.NextValue() * elapsedTime;
            TotalCountDict[counter.InstanceName] += (long)value;
        }

        _lastTimestamp = currentTimestamp;
    }
}

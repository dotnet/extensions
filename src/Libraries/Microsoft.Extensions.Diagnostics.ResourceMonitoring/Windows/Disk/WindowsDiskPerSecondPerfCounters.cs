// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDiskPerSecondPerfCounters
{
    private readonly List<IPerformanceCounter> _counters = [];
    private readonly IPerformanceCounterFactory _performanceCounterFactory;
    private readonly TimeProvider _timeProvider;
    private readonly string _categoryName;
    private readonly string _counterName;
    private readonly string[] _instanceNames;
    private long _lastTimestamp;

    internal WindowsDiskPerSecondPerfCounters(
        IPerformanceCounterFactory performanceCounterFactory,
        TimeProvider timeProvider,
        string categoryName,
        string counterName,
        string[] instanceNames)
    {
        _performanceCounterFactory = performanceCounterFactory;
        _timeProvider = timeProvider;
        _categoryName = categoryName;
        _counterName = counterName;
        _instanceNames = instanceNames;
    }

    /// <summary>
    /// Gets the disk I/O measurements.
    /// Key: Disk name, Value: Total count.
    /// </summary>
    internal IDictionary<string, long> TotalCountDict { get; } = new ConcurrentDictionary<string, long>();

    internal void InitializeDiskCounters()
    {
        foreach (string instanceName in _instanceNames)
        {
            // Skip the total instance
            if (instanceName.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Create counters for each disk
            _counters.Add(_performanceCounterFactory.Create(_categoryName, _counterName, instanceName));
            TotalCountDict.Add(instanceName, 0);
        }

        // Initialize the counters to get the first value
        foreach (IPerformanceCounter counter in _counters)
        {
            _ = counter.NextValue();
        }

        _lastTimestamp = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
    }

    internal void UpdateDiskCounters()
    {
        long currentTimestamp = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
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

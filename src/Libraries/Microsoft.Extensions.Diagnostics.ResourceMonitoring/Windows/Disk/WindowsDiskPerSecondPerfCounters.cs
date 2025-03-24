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
    private readonly List<PerformanceCounter> _counters = [];

    private readonly PerformanceCounterCategory _category;

    private readonly string _counterName;

    private long _lastTimestamp;

    internal WindowsDiskPerSecondPerfCounters(PerformanceCounterCategory category, string counterName)
    {
        _category = category;
        _counterName = counterName;
    }

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
            _counters.Add(new PerformanceCounter(_category.CategoryName, _counterName, instance));
            TotalCountDict.Add(instance, 0);
        }

        foreach (PerformanceCounter counter in _counters)
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

        foreach (PerformanceCounter counter in _counters)
        {
            double value = counter.NextValue() * elapsedTime;
            TotalCountDict[counter.InstanceName] += (long)value;
        }

        _lastTimestamp = currentTimestamp;
    }
}

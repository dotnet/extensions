// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

internal sealed class WindowsDiskIoTimePerfCounter
{
    private readonly List<IPerformanceCounter> _counters = [];
    private readonly IPerformanceCounterFactory _performanceCounterFactory;
    private readonly TimeProvider _timeProvider;
    private readonly string _categoryName;
    private readonly string _counterName;
    private readonly string[] _instanceNames;
    private long _lastTimeTicks;

    internal WindowsDiskIoTimePerfCounter(
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
    /// Gets the disk time measurements.
    /// Key: Disk name, Value: Real elapsed time used in busy state.
    /// </summary>
    internal IDictionary<string, double> TotalSeconds { get; } = new ConcurrentDictionary<string, double>();

    internal void InitializeDiskCounters()
    {
        foreach (string instanceName in _instanceNames)
        {
            // Create counters for each disk
            _counters.Add(_performanceCounterFactory.Create(_categoryName, _counterName, instanceName));
            TotalSeconds[instanceName] = 0f;
        }

        // Initialize the counters to get the first value
        foreach (IPerformanceCounter counter in _counters)
        {
            _ = counter.NextValue();
        }

        _lastTimeTicks = _timeProvider.GetUtcNow().Ticks;
    }

    internal void UpdateDiskCounters()
    {
        long currentTimeTicks = _timeProvider.GetUtcNow().Ticks;
        long elapsedTimeTicks = currentTimeTicks - _lastTimeTicks;

        // The real elapsed time ("wall clock") used in the I/O path (time from operations running in parallel are not counted).
        // Measured as the complement of "Disk\% Idle Time" performance counter: uptime * (100 - "Disk\% Idle Time") / 100
        // See https://opentelemetry.io/docs/specs/semconv/system/system-metrics/#metric-systemdiskio_time
        foreach (IPerformanceCounter counter in _counters)
        {
            // io busy time = (1 - (% idle time / 100)) * elapsed seconds
            float idleTimePercentage = Math.Min(counter.NextValue(), 100f);
            double busyTimeTicks = (1 - (idleTimePercentage / 100f)) * (double)elapsedTimeTicks;
            TotalSeconds[counter.InstanceName] += busyTimeTicks / TimeSpan.TicksPerSecond;
        }

        _lastTimeTicks = currentTimeTicks;
    }
}

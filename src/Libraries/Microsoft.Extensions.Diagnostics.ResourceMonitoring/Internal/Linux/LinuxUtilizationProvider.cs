// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metrics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class LinuxUtilizationProvider : ISnapshotProvider
{
    private const float Hundred = 100.0f;
    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly LinuxUtilizationParser _parser;
    private readonly ulong _totalMemoryInBytes;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;
    private readonly TimeProvider _timeProvider;
    private readonly double _scale;
    private readonly double _scaleForTrackerApi;

    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;

    private double _cpuPercentage = double.NaN;
    private double _memoryPercentage;
    private double _previousCgroupCpuTime;
    private double _previousHostCpuTime;

    public SystemResources Resources { get; }

    public LinuxUtilizationProvider(IOptions<ResourceMonitoringOptions> options, LinuxUtilizationParser parser,
        Meter<LinuxUtilizationProvider> meter, TimeProvider? timeProvider = null)
    {
        _parser = parser;
        _timeProvider = timeProvider ?? TimeProvider.System;
        var now = _timeProvider.GetUtcNow();
        _cpuRefreshInterval = options.Value.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.Value.MemoryConsumptionRefreshInterval;
        _refreshAfterCpu = now;
        _refreshAfterMemory = now;
        _totalMemoryInBytes = _parser.GetAvailableMemoryInBytes();
        _previousHostCpuTime = _parser.GetHostCpuUsageInNanoseconds();
        _previousCgroupCpuTime = _parser.GetCgroupCpuUsageInNanoseconds();

        var hostMemory = _parser.GetHostAvailableMemory();
        var hostCpus = _parser.GetHostCpuCount();
        var availableCpus = _parser.GetCgroupLimitedCpus();

        _scale = hostCpus * Hundred / availableCpus;
        _scaleForTrackerApi = hostCpus / availableCpus;

        _ = meter.CreateObservableGauge<double>(name: ResourceUtilizationCounters.CpuConsumptionPercentage, observeValue: CpuPercentage);
        _ = meter.CreateObservableGauge<double>(name: ResourceUtilizationCounters.MemoryConsumptionPercentage, observeValue: MemoryPercentage);

        Resources = new SystemResources(1, hostCpus, _totalMemoryInBytes, hostMemory);
    }

    public double CpuPercentage()
    {
        var now = _timeProvider.GetUtcNow();
        bool needUpdate = false;

        lock (_cpuLocker)
        {
            if (now >= _refreshAfterCpu)
            {
                needUpdate = true;
            }
        }

        if (needUpdate)
        {
            var hostCpuTime = _parser.GetHostCpuUsageInNanoseconds();
            var cgroupCpuTime = _parser.GetCgroupCpuUsageInNanoseconds();

            lock (_cpuLocker)
            {
                if (now >= _refreshAfterCpu)
                {
                    var deltaHost = hostCpuTime - _previousHostCpuTime;
                    var deltaCgroup = cgroupCpuTime - _previousCgroupCpuTime;

                    if (deltaHost > 0 && deltaCgroup > 0)
                    {
                        var percentage = Math.Min(Hundred, (deltaCgroup / deltaHost) * _scale);

                        _cpuPercentage = percentage;
                        _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                        _previousCgroupCpuTime = cgroupCpuTime;
                        _previousHostCpuTime = hostCpuTime;
                    }
                }
            }
        }

        return _cpuPercentage;
    }

    public double MemoryPercentage()
    {
        var now = _timeProvider.GetUtcNow();
        bool needUpdate = false;

        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                needUpdate = true;
            }
        }

        if (needUpdate)
        {
            var memoryUsed = _parser.GetMemoryUsageInBytes();

            lock (_memoryLocker)
            {
                if (now >= _refreshAfterMemory)
                {
                    var memoryPercentage = Math.Min(Hundred, ((double)memoryUsed / _totalMemoryInBytes) * Hundred);

                    _memoryPercentage = memoryPercentage;
                    _refreshAfterMemory = now.Add(_memoryRefreshInterval);
                }
            }
        }

        return _memoryPercentage;
    }

    /// <remarks>
    /// Not adding caching, to preserve original semantics of the code.
    /// The snapshot provider is called in intervals configured by the tracker.
    /// We multiply by scale to make hardcoded algorithm in tracker's calculator to produce right results.
    /// </remarks>
    public Snapshot GetSnapshot()
    {
        var hostTime = _parser.GetHostCpuUsageInNanoseconds();
        var cgroupTime = _parser.GetCgroupCpuUsageInNanoseconds();
        var memoryUsed = _parser.GetMemoryUsageInBytes();

        return new Snapshot(
            totalTimeSinceStart: TimeSpan.FromTicks(hostTime / (long)Hundred),
            kernelTimeSinceStart: TimeSpan.Zero,
            userTimeSinceStart: TimeSpan.FromTicks((long)(cgroupTime / (long)Hundred * _scaleForTrackerApi)),
            memoryUsageInBytes: memoryUsed);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

internal sealed class LinuxUtilizationProvider : ISnapshotProvider
{
    private const double One = 1.0;
    private const long Hundred = 100L;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly ILinuxUtilizationParser _parser;
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

    public LinuxUtilizationProvider(IOptions<ResourceMonitoringOptions> options, ILinuxUtilizationParser parser,
        IMeterFactory meterFactory, TimeProvider? timeProvider = null)
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
        var cpuGuaranteedRequest = _parser.GetCgroupRequestCpu();
        _scale = hostCpus / availableCpus;
        _scaleForTrackerApi = hostCpus / availableCpus;

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerCpuUtilization, observeValue: CpuUtilization, unit: "1");
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerMemoryUtilization, observeValue: MemoryUtilization, unit: "1");

        // cpuGuaranteedRequest is a CPU request for pod, for host its 1 core
        // available CPUs is a CPU limit for a pod or for a host.
        // _totalMemoryInBytes - Resource Memory Limit (in k8s terms)
        // _totalMemoryInBytes - To keep the contract, this parameter will get the Host available memory
        Resources = new SystemResources(cpuGuaranteedRequest, availableCpus, _totalMemoryInBytes, _totalMemoryInBytes);
    }

    public double CpuUtilization()
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
                        var percentage = Math.Min(One, deltaCgroup / deltaHost * _scale);

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

    public double MemoryUtilization()
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
                    var memoryPercentage = Math.Min(One, (double)memoryUsed / _totalMemoryInBytes);

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
            totalTimeSinceStart: TimeSpan.FromTicks(hostTime / Hundred),
            kernelTimeSinceStart: TimeSpan.Zero,
            userTimeSinceStart: TimeSpan.FromTicks((long)(cgroupTime / Hundred * _scaleForTrackerApi)),
            memoryUsageInBytes: memoryUsed);
    }
}

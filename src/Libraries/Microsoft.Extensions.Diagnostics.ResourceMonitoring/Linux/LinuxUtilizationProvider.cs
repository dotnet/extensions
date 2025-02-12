// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

internal sealed class LinuxUtilizationProvider : ISnapshotProvider
{
    private const double One = 1.0;
    private const long Hundred = 100L;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly ILogger<LinuxUtilizationProvider> _logger;
    private readonly ILinuxUtilizationParser _parser;
    private readonly ulong _memoryLimit;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;
    private readonly TimeProvider _timeProvider;
    private readonly double _scaleRelativeToCpuLimit;
    private readonly double _scaleRelativeToCpuRequest;
    private readonly double _scaleRelativeToCpuRequestForTrackerApi;

    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;

    private double _cpuPercentage = double.NaN;
    private double _memoryPercentage;
    private long _previousCgroupCpuTime;
    private long _previousHostCpuTime;

    public SystemResources Resources { get; }

    public LinuxUtilizationProvider(IOptions<ResourceMonitoringOptions> options, ILinuxUtilizationParser parser,
        IMeterFactory meterFactory, ILogger<LinuxUtilizationProvider>? logger = null, TimeProvider? timeProvider = null)
    {
        _parser = parser;
        _logger = logger ?? NullLogger<LinuxUtilizationProvider>.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        _cpuRefreshInterval = options.Value.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.Value.MemoryConsumptionRefreshInterval;
        _refreshAfterCpu = now;
        _refreshAfterMemory = now;
        _memoryLimit = _parser.GetAvailableMemoryInBytes();
        _previousHostCpuTime = _parser.GetHostCpuUsageInNanoseconds();
        _previousCgroupCpuTime = _parser.GetCgroupCpuUsageInNanoseconds();

        float hostCpus = _parser.GetHostCpuCount();
        float cpuLimit = _parser.GetCgroupLimitedCpus();
        float cpuRequest = _parser.GetCgroupRequestCpu();
        _scaleRelativeToCpuLimit = hostCpus / cpuLimit;
        _scaleRelativeToCpuRequest = hostCpus / cpuRequest;
        _scaleRelativeToCpuRequestForTrackerApi = hostCpus; // the division by cpuRequest is performed later on in the ResourceUtilization class

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerCpuLimitUtilization, observeValue: () => CpuUtilization() * _scaleRelativeToCpuLimit, unit: "1");
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, observeValue: MemoryUtilization, unit: "1");
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerCpuRequestUtilization, observeValue: () => CpuUtilization() * _scaleRelativeToCpuRequest, unit: "1");

        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessCpuUtilization, observeValue: () => CpuUtilization() * _scaleRelativeToCpuRequest, unit: "1");
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessMemoryUtilization, observeValue: MemoryUtilization, unit: "1");

        // cpuRequest is a CPU request (aka guaranteed number of CPU units) for pod, for host its 1 core
        // cpuLimit is a CPU limit (aka max CPU units available) for a pod or for a host.
        // _memoryLimit - Resource Memory Limit (in k8s terms)
        // _memoryLimit - To keep the contract, this parameter will get the Host available memory
        Resources = new SystemResources(cpuRequest, cpuLimit, _memoryLimit, _memoryLimit);
        Log.SystemResourcesInfo(_logger, cpuLimit, cpuRequest, _memoryLimit, _memoryLimit);
    }

    public double CpuUtilization()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        lock (_cpuLocker)
        {
            if (now < _refreshAfterCpu)
            {
                return _cpuPercentage;
            }
        }

        long hostCpuTime = _parser.GetHostCpuUsageInNanoseconds();
        long cgroupCpuTime = _parser.GetCgroupCpuUsageInNanoseconds();

        lock (_cpuLocker)
        {
            if (now >= _refreshAfterCpu)
            {
                long deltaHost = hostCpuTime - _previousHostCpuTime;
                long deltaCgroup = cgroupCpuTime - _previousCgroupCpuTime;

                if (deltaHost > 0 && deltaCgroup > 0)
                {
                    double percentage = Math.Min(One, (double)deltaCgroup / deltaHost);

                    Log.CpuUsageData(_logger, cgroupCpuTime, hostCpuTime, _previousCgroupCpuTime, _previousHostCpuTime, percentage);

                    _cpuPercentage = percentage;
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                    _previousCgroupCpuTime = cgroupCpuTime;
                    _previousHostCpuTime = hostCpuTime;
                }
            }
        }

        return _cpuPercentage;
    }

    public double MemoryUtilization()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        lock (_memoryLocker)
        {
            if (now < _refreshAfterMemory)
            {
                return _memoryPercentage;
            }
        }

        ulong memoryUsed = _parser.GetMemoryUsageInBytes();

        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                double memoryPercentage = Math.Min(One, (double)memoryUsed / _memoryLimit);

                _memoryPercentage = memoryPercentage;
                _refreshAfterMemory = now.Add(_memoryRefreshInterval);
            }
        }

        Log.MemoryUsageData(_logger, memoryUsed, _memoryLimit, _memoryPercentage);

        return _memoryPercentage;
    }

    /// <remarks>
    /// Not adding caching, to preserve original semantics of the code.
    /// The snapshot provider is called in intervals configured by the tracker.
    /// We multiply by scale to make hardcoded algorithm in tracker's calculator to produce right results.
    /// </remarks>
    public Snapshot GetSnapshot()
    {
        long hostTime = _parser.GetHostCpuUsageInNanoseconds();
        long cgroupTime = _parser.GetCgroupCpuUsageInNanoseconds();
        ulong memoryUsed = _parser.GetMemoryUsageInBytes();

        return new Snapshot(
            totalTimeSinceStart: TimeSpan.FromTicks(hostTime / Hundred),
            kernelTimeSinceStart: TimeSpan.Zero,
            userTimeSinceStart: TimeSpan.FromTicks((long)(cgroupTime / Hundred * _scaleRelativeToCpuRequestForTrackerApi)),
            memoryUsageInBytes: memoryUsed);
    }
}

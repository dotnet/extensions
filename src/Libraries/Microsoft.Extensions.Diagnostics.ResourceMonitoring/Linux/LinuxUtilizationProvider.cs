// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
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
    private readonly long _cpuPeriodsInterval;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;
    private readonly TimeProvider _timeProvider;
    private readonly double _scaleRelativeToCpuRequestForTrackerApi;

    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);
    private DateTimeOffset _lastFailure = DateTimeOffset.MinValue;
    private int _measurementsUnavailable;

    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;
    private double _cpuPercentage = double.NaN;
    private double _lastCpuCoresUsed = double.NaN;
    private double _memoryPercentage;
    private long _previousCgroupCpuTime;
    private long _previousHostCpuTime;
    private long _previousCgroupCpuPeriodCounter;
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
        float scaleRelativeToCpuLimit = hostCpus / cpuLimit;
        float scaleRelativeToCpuRequest = hostCpus / cpuRequest;
        _scaleRelativeToCpuRequestForTrackerApi = hostCpus; // the division by cpuRequest is performed later on in the ResourceUtilization class

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        if (options.Value.UseLinuxCalculationV2)
        {
            cpuLimit = _parser.GetCgroupLimitV2();
            cpuRequest = _parser.GetCgroupRequestCpuV2();

            // Get Cpu periods interval from cgroup
            _cpuPeriodsInterval = _parser.GetCgroupPeriodsIntervalInMicroSecondsV2();
            (_previousCgroupCpuTime, _previousCgroupCpuPeriodCounter) = _parser.GetCgroupCpuUsageInNanosecondsAndCpuPeriodsV2();

            _ = meter.CreateObservableGauge(
                ResourceUtilizationInstruments.ContainerCpuLimitUtilization,
                () => GetMeasurementWithRetry(() => CpuUtilizationLimit(cpuLimit)),
                "1");

            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ContainerCpuRequestUtilization,
                observeValues: () => GetMeasurementWithRetry(() => CpuUtilizationRequest(cpuRequest)),
                unit: "1");
        }
        else
        {
            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ContainerCpuLimitUtilization,
                observeValues: () => GetMeasurementWithRetry(() => CpuUtilization() * scaleRelativeToCpuLimit),
                unit: "1");

            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ContainerCpuRequestUtilization,
                observeValues: () => GetMeasurementWithRetry(() => CpuUtilization() * scaleRelativeToCpuRequest),
                unit: "1");

            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ProcessCpuUtilization,
                observeValues: () => GetMeasurementWithRetry(() => CpuUtilization() * scaleRelativeToCpuRequest),
                unit: "1");
        }

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ContainerMemoryLimitUtilization,
            observeValues: () => GetMeasurementWithRetry(() => MemoryUtilization()),
            unit: "1");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ProcessMemoryUtilization,
            observeValues: () => GetMeasurementWithRetry(() => MemoryUtilization()),
            unit: "1");

        // cpuRequest is a CPU request (aka guaranteed number of CPU units) for pod, for host its 1 core
        // cpuLimit is a CPU limit (aka max CPU units available) for a pod or for a host.
        // _memoryLimit - Resource Memory Limit (in k8s terms)
        // _memoryLimit - To keep the contract, this parameter will get the Host available memory
        Resources = new SystemResources(cpuRequest, cpuLimit, _memoryLimit, _memoryLimit);
        _logger.SystemResourcesInfo(cpuLimit, cpuRequest, _memoryLimit, _memoryLimit);
    }

    public double CpuUtilizationV2()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        lock (_cpuLocker)
        {
            if (now < _refreshAfterCpu)
            {
                return _lastCpuCoresUsed;
            }
        }

        (long cpuUsageTime, long cpuPeriodCounter) = _parser.GetCgroupCpuUsageInNanosecondsAndCpuPeriodsV2();
        lock (_cpuLocker)
        {
            if (now < _refreshAfterCpu)
            {
                return _lastCpuCoresUsed;
            }

            long deltaCgroup = cpuUsageTime - _previousCgroupCpuTime;
            long deltaPeriodCount = cpuPeriodCounter - _previousCgroupCpuPeriodCounter;

            if (deltaCgroup <= 0 || deltaPeriodCount <= 0)
            {
                return _lastCpuCoresUsed;
            }

            long deltaCpuPeriodInNanoseconds = deltaPeriodCount * _cpuPeriodsInterval * 1000;
            double coresUsed = deltaCgroup / (double)deltaCpuPeriodInNanoseconds;

            _logger.CpuUsageDataV2(cpuUsageTime, _previousCgroupCpuTime, deltaCpuPeriodInNanoseconds, coresUsed);

            _lastCpuCoresUsed = coresUsed;
            _refreshAfterCpu = now.Add(_cpuRefreshInterval);
            _previousCgroupCpuTime = cpuUsageTime;
            _previousCgroupCpuPeriodCounter = cpuPeriodCounter;
        }

        return _lastCpuCoresUsed;
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
            if (now < _refreshAfterCpu)
            {
                return _cpuPercentage;
            }

            long deltaHost = hostCpuTime - _previousHostCpuTime;
            long deltaCgroup = cgroupCpuTime - _previousCgroupCpuTime;

            if (deltaHost <= 0 || deltaCgroup <= 0)
            {
                return _cpuPercentage;
            }

            double percentage = Math.Min(One, (double)deltaCgroup / deltaHost);

            _logger.CpuUsageData(cgroupCpuTime, hostCpuTime, _previousCgroupCpuTime, _previousHostCpuTime, percentage);

            _cpuPercentage = percentage;
            _refreshAfterCpu = now.Add(_cpuRefreshInterval);
            _previousCgroupCpuTime = cgroupCpuTime;
            _previousHostCpuTime = hostCpuTime;
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

        _logger.MemoryUsageData(memoryUsed, _memoryLimit, _memoryPercentage);

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

    private IEnumerable<Measurement<double>> GetMeasurementWithRetry(Func<double> func)
    {
        if (Volatile.Read(ref _measurementsUnavailable) == 1 &&
            _timeProvider.GetUtcNow() - _lastFailure < _retryInterval)
        {
            return Enumerable.Empty<Measurement<double>>();
        }

        try
        {
            double result = func();
            if (Volatile.Read(ref _measurementsUnavailable) == 1)
            {
                _ = Interlocked.Exchange(ref _measurementsUnavailable, 0);
            }

            return new[] { new Measurement<double>(result) };
        }
        catch (Exception ex) when (
            ex is System.IO.FileNotFoundException ||
            ex is System.IO.DirectoryNotFoundException ||
            ex is System.UnauthorizedAccessException)
        {
            _lastFailure = _timeProvider.GetUtcNow();
            _ = Interlocked.Exchange(ref _measurementsUnavailable, 1);

            return Enumerable.Empty<Measurement<double>>();
        }
    }

    // Math.Min() is used below to mitigate margin errors and various kinds of precisions losses
    // due to the fact that the calculation itself is not an atomic operation:
    private double CpuUtilizationRequest(double cpuRequest) => Math.Min(One, CpuUtilizationV2() / cpuRequest);
    private double CpuUtilizationLimit(double cpuLimit) => Math.Min(One, CpuUtilizationV2() / cpuLimit);

    private IEnumerable<Measurement<double>> GetCpuTime()
    {
        long hostCpuTime = _parser.GetHostCpuUsageInNanoseconds();
        double cgroupCpuTime = CpuUtilizationWithoutHostDelta();

        yield return new Measurement<double>(cgroupCpuTime / NanosecondsInSecond, [new KeyValuePair<string, object?>("cpu.mode", "user")]);
        yield return new Measurement<double>(hostCpuTime / NanosecondsInSecond, [new KeyValuePair<string, object?>("cpu.mode", "system")]);
    }
}

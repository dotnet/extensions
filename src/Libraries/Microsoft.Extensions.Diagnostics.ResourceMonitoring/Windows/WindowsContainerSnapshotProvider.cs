﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsContainerSnapshotProvider : ISnapshotProvider
{
    private const double One = 1.0d;
    private const double Hundred = 100.0d;
    private const double TicksPerSecondDouble = TimeSpan.TicksPerSecond;

    /// <summary>
    /// This represents a factory method for creating the JobHandle.
    /// </summary>
    private readonly Func<IJobHandle> _createJobHandleObject;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly object _processMemoryLocker = new();
    private readonly TimeProvider _timeProvider;
    private readonly IProcessInfo _processInfo;
    private readonly ILogger<WindowsContainerSnapshotProvider> _logger;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;
    private readonly double _metricValueMultiplier;

    private double _memoryLimit;
    private double _cpuLimit;
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables. Those will be used once we bring relevant meters.
    private ulong _memoryRequest;
    private double _cpuRequest;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    private long _oldCpuUsageTicks;
    private long _oldCpuTimeTicks;
    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;
    private DateTimeOffset _refreshAfterProcessMemory;
    private double _cpuPercentage = double.NaN;
    private double _processMemoryPercentage;
    private ulong _memoryUsage;

    public SystemResources Resources { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    public WindowsContainerSnapshotProvider(
        ILogger<WindowsContainerSnapshotProvider>? logger,
        IMeterFactory meterFactory,
        IOptions<ResourceMonitoringOptions> options,
        IResourceQuotaProvider resourceQuotaProvider)
        : this(new ProcessInfo(), logger, meterFactory,
              static () => new JobHandleWrapper(), TimeProvider.System, options.Value, resourceQuotaProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    /// <remarks>This constructor enables the mocking of <see cref="WindowsContainerSnapshotProvider"/> dependencies for the purpose of Unit Testing only.</remarks>
    internal WindowsContainerSnapshotProvider(
        IProcessInfo processInfo,
        ILogger<WindowsContainerSnapshotProvider>? logger,
        IMeterFactory meterFactory,
        Func<IJobHandle> createJobHandleObject,
        TimeProvider timeProvider,
        ResourceMonitoringOptions options,
        IResourceQuotaProvider resourceQuotaProvider)
    {
        _logger = logger ?? NullLogger<WindowsContainerSnapshotProvider>.Instance;
        _logger.RunningInsideJobObject();

        _metricValueMultiplier = options.UseZeroToOneRangeForMetrics ? One : Hundred;

        _createJobHandleObject = createJobHandleObject;
        _processInfo = processInfo;

        _timeProvider = timeProvider;

        using IJobHandle jobHandle = _createJobHandleObject();

        var quota = resourceQuotaProvider.GetResourceQuota();
        _cpuLimit = quota.LimitsCpu;
        _memoryLimit = quota.LimitsMemory;
        _cpuRequest = quota.RequestsCpu;
        _memoryRequest = quota.RequestsMemory;

        ulong memoryLimitRounded = (ulong)Math.Round(_memoryLimit);
        Resources = new SystemResources(_cpuRequest, _cpuLimit, _memoryRequest, memoryLimitRounded);
        _logger.SystemResourcesInfo(_cpuLimit, _cpuRequest, memoryLimitRounded, _memoryRequest);

        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();
        _oldCpuUsageTicks = basicAccountingInfo.TotalKernelTime + basicAccountingInfo.TotalUserTime;
        _oldCpuTimeTicks = _timeProvider.GetUtcNow().Ticks;
        _cpuRefreshInterval = options.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.MemoryConsumptionRefreshInterval;
        _refreshAfterCpu = _timeProvider.GetUtcNow();
        _refreshAfterMemory = _timeProvider.GetUtcNow();
        _refreshAfterProcessMemory = _timeProvider.GetUtcNow();

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        Meter meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableCounter(
            name: ResourceUtilizationInstruments.ContainerCpuTime,
            observeValues: GetCpuTime,
            unit: "s",
            description: "CPU time used by the container.");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ContainerCpuLimitUtilization,
            observeValue: CpuPercentage);

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ContainerMemoryLimitUtilization,
            observeValue: () => Math.Min(_metricValueMultiplier, MemoryUsage() / _memoryLimit * _metricValueMultiplier));

        _ = meter.CreateObservableUpDownCounter(
            name: ResourceUtilizationInstruments.ContainerMemoryUsage,
            observeValue: () => (long)MemoryUsage(),
            unit: "By",
            description: "Memory usage of the container.");

        // Process based metrics:
        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ProcessCpuUtilization,
            observeValue: CpuPercentage);

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ProcessMemoryUtilization,
            observeValue: ProcessMemoryPercentage);
    }

    public Snapshot GetSnapshot()
    {
        // Gather the information
        // Cpu kernel and user ticks
        using var jobHandle = _createJobHandleObject();
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();

        return new Snapshot(
            TimeSpan.FromTicks(_timeProvider.GetUtcNow().Ticks),
            TimeSpan.FromTicks(basicAccountingInfo.TotalKernelTime),
            TimeSpan.FromTicks(basicAccountingInfo.TotalUserTime),
            _processInfo.GetCurrentProcessMemoryUsage());
    }

    private double ProcessMemoryPercentage()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        lock (_processMemoryLocker)
        {
            if (now < _refreshAfterProcessMemory)
            {
                return _processMemoryPercentage;
            }
        }

        ulong processMemoryUsage = _processInfo.GetCurrentProcessMemoryUsage();

        lock (_processMemoryLocker)
        {
            if (now >= _refreshAfterProcessMemory)
            {
                _processMemoryPercentage = Math.Min(_metricValueMultiplier, processMemoryUsage / _memoryLimit * _metricValueMultiplier);
                _refreshAfterProcessMemory = now.Add(_memoryRefreshInterval);

                _logger.ProcessMemoryPercentageData(processMemoryUsage, _memoryLimit, _processMemoryPercentage);
            }

            return _processMemoryPercentage;
        }
    }

    private ulong MemoryUsage()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        lock (_memoryLocker)
        {
            if (now < _refreshAfterMemory)
            {
                return _memoryUsage;
            }
        }

        ulong memoryUsage = _processInfo.GetMemoryUsage();

        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                _memoryUsage = memoryUsage;
                _refreshAfterMemory = now.Add(_memoryRefreshInterval);
                _logger.ContainerMemoryUsageData(_memoryUsage, _memoryLimit);
            }

            return _memoryUsage;
        }
    }

    private IEnumerable<Measurement<double>> GetCpuTime()
    {
        using IJobHandle jobHandle = _createJobHandleObject();
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();

        yield return new Measurement<double>(basicAccountingInfo.TotalUserTime / TicksPerSecondDouble,
            [new KeyValuePair<string, object?>("cpu.mode", "user")]);
        yield return new Measurement<double>(basicAccountingInfo.TotalKernelTime / TicksPerSecondDouble,
            [new KeyValuePair<string, object?>("cpu.mode", "system")]);
    }

    private double CpuPercentage()
    {
        var now = _timeProvider.GetUtcNow();

        lock (_cpuLocker)
        {
            if (now < _refreshAfterCpu)
            {
                return _cpuPercentage;
            }
        }

        using var jobHandle = _createJobHandleObject();
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();
        var currentCpuTicks = basicAccountingInfo.TotalKernelTime + basicAccountingInfo.TotalUserTime;

        lock (_cpuLocker)
        {
            if (now >= _refreshAfterCpu)
            {
                var usageTickDelta = currentCpuTicks - _oldCpuUsageTicks;
                var timeTickDelta = (now.Ticks - _oldCpuTimeTicks) * _cpuLimit;
                if (usageTickDelta > 0 && timeTickDelta > 0)
                {
                    // Don't change calculation order, otherwise precision is lost:
                    _cpuPercentage = Math.Min(_metricValueMultiplier, usageTickDelta / timeTickDelta * _metricValueMultiplier);

                    _logger.CpuContainerUsageData(
                        basicAccountingInfo.TotalKernelTime, basicAccountingInfo.TotalUserTime, _oldCpuUsageTicks, timeTickDelta, _cpuLimit, _cpuPercentage);

                    _oldCpuUsageTicks = currentCpuTicks;
                    _oldCpuTimeTicks = now.Ticks;
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                }
            }

            return _cpuPercentage;
        }
    }
}

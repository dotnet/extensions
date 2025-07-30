// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

/// <summary>
/// Unified Windows container snapshot provider that works with different data sources.
/// </summary>
internal sealed class UnifiedWindowsContainerSnapshotProvider : ISnapshotProvider
{
    private const double One = 1.0d;
    private const double Hundred = 100.0d;
    private const double TicksPerSecondDouble = TimeSpan.TicksPerSecond;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UnifiedWindowsContainerSnapshotProvider> _logger;
    private readonly IWindowsResourceDataProvider _resourceDataProvider;
    private readonly WindowsResourceLimits _resourceLimits;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;
    private readonly double _metricValueMultiplier;

    private long _oldCpuUsageTicks;
    private long _oldCpuTimeTicks;
    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;
    private double _cpuPercentage = double.NaN;
    private double _memoryPercentage;

    public SystemResources Resources { get; }

    public UnifiedWindowsContainerSnapshotProvider(
        ILogger<UnifiedWindowsContainerSnapshotProvider>? logger,
        IMeterFactory meterFactory,
        TimeProvider timeProvider,
        IWindowsResourceDataProvider resourceDataProvider,
        ResourceMonitoringOptions options)
    {
        _logger = logger ?? NullLogger<UnifiedWindowsContainerSnapshotProvider>.Instance;
        _timeProvider = timeProvider;
        _resourceDataProvider = resourceDataProvider;
        _resourceLimits = resourceDataProvider.GetResourceLimits();

        _metricValueMultiplier = options.UseZeroToOneRangeForMetrics ? One : Hundred;
        _cpuRefreshInterval = options.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.MemoryConsumptionRefreshInterval;

        Resources = new SystemResources(
            _resourceLimits.CpuRequest,
            _resourceLimits.CpuLimit,
            _resourceLimits.MemoryRequest,
            _resourceLimits.MemoryLimit);

        _logger.LogInformation("Container resource limits initialized. CPU: {CpuLimit}, Memory: {MemoryLimit}MB, HasRequests: {HasRequests}",
            _resourceLimits.CpuLimit, _resourceLimits.MemoryLimit / (1024 * 1024), _resourceLimits.HasRequests);

        // Initialize tracking variables
        _oldCpuUsageTicks = _resourceDataProvider.GetCurrentCpuTicks();
        _oldCpuTimeTicks = _timeProvider.GetUtcNow().Ticks;
        _refreshAfterCpu = _timeProvider.GetUtcNow();
        _refreshAfterMemory = _timeProvider.GetUtcNow();

        RegisterMetrics(meterFactory);
    }

    public Snapshot GetSnapshot()
    {
        var (userTime, kernelTime) = _resourceDataProvider.GetCpuTimeBreakdown();
        var memoryUsage = _resourceDataProvider.GetCurrentMemoryUsage();

        return new Snapshot(
            TimeSpan.FromTicks(_timeProvider.GetUtcNow().Ticks),
            TimeSpan.FromTicks(kernelTime),
            TimeSpan.FromTicks(userTime),
            memoryUsage);
    }

    private void RegisterMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        // Always register these metrics
        _ = meter.CreateObservableCounter(
            name: ResourceUtilizationInstruments.ContainerCpuTime,
            observeValues: GetCpuTime,
            unit: "s",
            description: "CPU time used by the container.");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ContainerCpuLimitUtilization,
            observeValue: () => CpuPercentage(_resourceLimits.CpuLimit),
            description: "CPU utilization percentage against container CPU limits.");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ContainerMemoryLimitUtilization,
            observeValue: () => MemoryPercentage(_resourceLimits.MemoryLimit),
            description: "Memory utilization percentage against container memory limits.");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ProcessCpuUtilization,
            observeValue: () => CpuPercentage(_resourceLimits.CpuLimit),
            description: "Process CPU utilization.");

        _ = meter.CreateObservableGauge(
            name: ResourceUtilizationInstruments.ProcessMemoryUtilization,
            observeValue: () => MemoryPercentage(_resourceLimits.MemoryLimit),
            description: "Process memory utilization.");

        // Only register request metrics when we have meaningful request values
        if (_resourceLimits.HasRequests)
        {
            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ContainerCpuRequestUtilization,
                observeValue: () => CpuPercentage(_resourceLimits.CpuRequest),
                description: "CPU utilization percentage against container CPU requests.");

            _ = meter.CreateObservableGauge(
                name: ResourceUtilizationInstruments.ContainerMemoryRequestUtilization,
                observeValue: () => MemoryPercentage(_resourceLimits.MemoryRequest),
                description: "Memory utilization percentage against container memory requests.");
        }
    }

    private double CpuPercentage(double cpuLimit)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_cpuLocker)
        {
            if (now < _refreshAfterCpu)
            {
                return _cpuPercentage;
            }
        }

        var currentCpuTicks = _resourceDataProvider.GetCurrentCpuTicks();

        lock (_cpuLocker)
        {
            if (now >= _refreshAfterCpu)
            {
                var usageTickDelta = currentCpuTicks - _oldCpuUsageTicks;
                var timeTickDelta = (now.Ticks - _oldCpuTimeTicks) * cpuLimit;

                if (usageTickDelta > 0 && timeTickDelta > 0)
                {
                    _cpuPercentage = Math.Min(_metricValueMultiplier, usageTickDelta / timeTickDelta * _metricValueMultiplier);

                    _oldCpuUsageTicks = currentCpuTicks;
                    _oldCpuTimeTicks = now.Ticks;
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                }
            }

            return _cpuPercentage;
        }
    }

    private double MemoryPercentage(ulong memoryLimit)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_memoryLocker)
        {
            if (now < _refreshAfterMemory)
            {
                return _memoryPercentage;
            }
        }

        var memoryUsage = _resourceDataProvider.GetCurrentMemoryUsage();

        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                if (memoryLimit > 0)
                {
                    _memoryPercentage = Math.Min(_metricValueMultiplier, memoryUsage / (double)memoryLimit * _metricValueMultiplier);
                }

                _refreshAfterMemory = now.Add(_memoryRefreshInterval);
            }

            _logger.LogDebug("Memory usage: {MemoryUsage}MB, Limit: {MemoryLimit}MB, Percentage: {MemoryPercentage}%",
                memoryUsage / (1024 * 1024), memoryLimit / (1024 * 1024), _memoryPercentage);

            return _memoryPercentage;
        }
    }

    private IEnumerable<Measurement<double>> GetCpuTime()
    {
        var (userTime, kernelTime) = _resourceDataProvider.GetCpuTimeBreakdown();

        yield return new Measurement<double>(userTime / TicksPerSecondDouble,
            [new KeyValuePair<string, object?>("cpu.mode", "user")]);
        yield return new Measurement<double>(kernelTime / TicksPerSecondDouble,
            [new KeyValuePair<string, object?>("cpu.mode", "system")]);
    }
}

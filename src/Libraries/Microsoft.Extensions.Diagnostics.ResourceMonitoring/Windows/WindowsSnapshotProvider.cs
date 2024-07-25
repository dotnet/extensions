// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsSnapshotProvider : ISnapshotProvider
{
    private const double Hundred = 100.0d;

    public SystemResources Resources { get; }

    private readonly int _cpuUnits;
    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly TimeProvider _timeProvider;
    private readonly Func<long> _getCpuTicksFunc;
    private readonly Func<long> _getMemoryUsageFunc;
    private readonly double _totalMemory;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;

    private long _oldCpuUsageTicks;
    private long _oldCpuTimeTicks;
    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;
    private double _cpuPercentage = double.NaN;
    private double _memoryPercentage;

    public WindowsSnapshotProvider(ILogger<WindowsSnapshotProvider> logger, IMeterFactory meterFactory, IOptions<ResourceMonitoringOptions> options)
    : this(logger, meterFactory, options.Value, TimeProvider.System, GetCpuUnits, GetCpuTicks, GetMemoryUsageInBytes, GetTotalMemoryInBytes)
    {
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Dependencies for testing")]
    internal WindowsSnapshotProvider(
        ILogger<WindowsSnapshotProvider> logger,
        IMeterFactory meterFactory,
        ResourceMonitoringOptions options,
        TimeProvider timeProvider,
        Func<int> getCpuUnitsFunc,
        Func<long> getCpuTicksFunc,
        Func<long> getMemoryUsageFunc,
        Func<ulong> getTotalMemoryInBytesFunc)
    {
        Log.RunningOutsideJobObject(logger);

        _cpuUnits = getCpuUnitsFunc();
        var totalMemory = getTotalMemoryInBytesFunc();
        Resources = new SystemResources(_cpuUnits, _cpuUnits, totalMemory, totalMemory);

        _timeProvider = timeProvider;
        _getCpuTicksFunc = getCpuTicksFunc;
        _getMemoryUsageFunc = getMemoryUsageFunc;
        _totalMemory = totalMemory; // "long" totalMemory => "double" _totalMemory - to calculate percentage later

        _oldCpuUsageTicks = getCpuTicksFunc();
        _oldCpuTimeTicks = timeProvider.GetUtcNow().Ticks;
        _cpuRefreshInterval = options.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.MemoryConsumptionRefreshInterval;
        _refreshAfterCpu = timeProvider.GetUtcNow();
        _refreshAfterMemory = timeProvider.GetUtcNow();

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessCpuUtilization, observeValue: CpuPercentage);
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessMemoryUtilization, observeValue: MemoryPercentage);
    }

    public Snapshot GetSnapshot()
    {
        using var process = Process.GetCurrentProcess();

        return new Snapshot(
            totalTimeSinceStart: TimeSpan.FromTicks(_timeProvider.GetUtcNow().Ticks),
            kernelTimeSinceStart: process.PrivilegedProcessorTime,
            userTimeSinceStart: process.UserProcessorTime,
            memoryUsageInBytes: (ulong)process.WorkingSet64);
    }

    internal static long GetCpuTicks()
    {
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.Ticks;
    }

    internal static int GetCpuUnits() => Environment.ProcessorCount;

    internal static long GetMemoryUsageInBytes()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    internal static ulong GetTotalMemoryInBytes()
    {
        var memoryStatus = new MemoryInfo().GetMemoryStatus();
        return memoryStatus.TotalPhys;
    }

    private double MemoryPercentage()
    {
        var now = _timeProvider.GetUtcNow();

        lock (_memoryLocker)
        {
            if (now < _refreshAfterMemory)
            {
                return _memoryPercentage;
            }
        }

        var currentMemoryUsage = _getMemoryUsageFunc();
        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                _memoryPercentage = Math.Min(Hundred, currentMemoryUsage / _totalMemory * Hundred); // Don't change calculation order, otherwise we loose some precision
                _refreshAfterMemory = now.Add(_memoryRefreshInterval);
            }

            return _memoryPercentage;
        }
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

        var currentCpuTicks = _getCpuTicksFunc();

        lock (_cpuLocker)
        {
            if (now >= _refreshAfterCpu)
            {
                var usageTickDelta = currentCpuTicks - _oldCpuUsageTicks;
                var timeTickDelta = (now.Ticks - _oldCpuTimeTicks) * _cpuUnits;
                if (usageTickDelta > 0 && timeTickDelta > 0)
                {
                    _oldCpuUsageTicks = currentCpuTicks;
                    _oldCpuTimeTicks = now.Ticks;
                    _cpuPercentage = Math.Min(Hundred, usageTickDelta / (double)timeTickDelta * Hundred); // Don't change calculation order, otherwise we loose some precision
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                }
            }

            return _cpuPercentage;
        }
    }
}

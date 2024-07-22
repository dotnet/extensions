﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsContainerSnapshotProvider : ISnapshotProvider
{
    private const double Hundred = 100.0d;

    private readonly Lazy<MEMORYSTATUSEX> _memoryStatus;

    /// <summary>
    /// This represents a factory method for creating the JobHandle.
    /// </summary>
    private readonly Func<IJobHandle> _createJobHandleObject;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly TimeProvider _timeProvider;
    private readonly IProcessInfo _processInfo;
    private readonly double _totalMemory;
    private readonly double _cpuUnits;
    private readonly TimeSpan _cpuRefreshInterval;
    private readonly TimeSpan _memoryRefreshInterval;

    private long _oldCpuUsageTicks;
    private long _oldCpuTimeTicks;
    private DateTimeOffset _refreshAfterCpu;
    private DateTimeOffset _refreshAfterMemory;
    private double _cpuPercentage = double.NaN;
    private double _memoryPercentage;

    public SystemResources Resources { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    public WindowsContainerSnapshotProvider(
        ILogger<WindowsContainerSnapshotProvider> logger,
        IMeterFactory meterFactory,
        IOptions<ResourceMonitoringOptions> options)
        : this(new MemoryInfo(), new SystemInfo(), new ProcessInfo(), logger, meterFactory,
              static () => new JobHandleWrapper(), TimeProvider.System, options.Value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    /// <remarks>This constructor enables the mocking of <see cref="WindowsContainerSnapshotProvider"/> dependencies for the purpose of Unit Testing only.</remarks>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Dependencies for testing")]
    internal WindowsContainerSnapshotProvider(
        IMemoryInfo memoryInfo,
        ISystemInfo systemInfo,
        IProcessInfo processInfo,
        ILogger<WindowsContainerSnapshotProvider> logger,
        IMeterFactory meterFactory,
        Func<IJobHandle> createJobHandleObject,
        TimeProvider timeProvider,
        ResourceMonitoringOptions options)
    {
        Log.RunningInsideJobObject(logger);

        _memoryStatus = new Lazy<MEMORYSTATUSEX>(
            memoryInfo.GetMemoryStatus,
            LazyThreadSafetyMode.ExecutionAndPublication);
        _createJobHandleObject = createJobHandleObject;
        _processInfo = processInfo;

        _timeProvider = timeProvider;

        // initialize system resources information
        using var jobHandle = _createJobHandleObject();

        _cpuUnits = GetGuaranteedCpuUnits(jobHandle, systemInfo);
        var memory = GetMemoryLimits(jobHandle);

        Resources = new SystemResources(_cpuUnits, _cpuUnits, memory, memory);

        _totalMemory = memory;
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();
        _oldCpuUsageTicks = basicAccountingInfo.TotalKernelTime + basicAccountingInfo.TotalUserTime;
        _oldCpuTimeTicks = _timeProvider.GetUtcNow().Ticks;
        _cpuRefreshInterval = options.CpuConsumptionRefreshInterval;
        _memoryRefreshInterval = options.MemoryConsumptionRefreshInterval;
        _refreshAfterCpu = _timeProvider.GetUtcNow();
        _refreshAfterMemory = _timeProvider.GetUtcNow();

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
#pragma warning restore CA2000 // Dispose objects before losing scope

        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.CpuUtilization, observeValue: CpuPercentage);
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.MemoryUtilization, observeValue: MemoryPercentage);
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
            GetMemoryUsage());
    }

    private static double GetGuaranteedCpuUnits(IJobHandle jobHandle, ISystemInfo systemInfo)
    {
        // Note: This function convert the CpuRate from CPU cycles to CPU units, also it scales
        // the CPU units with the number of processors (cores) available in the system.
        const double CpuCycles = 10_000U;

        var cpuLimit = jobHandle.GetJobCpuLimitInfo();
        double cpuRatio = 1.0;
        if ((cpuLimit.ControlFlags & (uint)JobObjectInfo.JobCpuRateControlLimit.CpuRateControlEnable) != 0 &&
            (cpuLimit.ControlFlags & (uint)JobObjectInfo.JobCpuRateControlLimit.CpuRateControlHardCap) != 0)
        {
            // The CpuRate is represented as number of cycles during scheduling interval, where
            // a full cpu cycles number would equal 10_000, so for example if the CpuRate is 2_000,
            // that means that the application (or container) is assigned 20% of the total CPU available.
            // So, here we divide the CpuRate by 10_000 to convert it to a ratio (ex: 0.2 for 20% CPU).
            // For more info: https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_cpu_rate_control_information?redirectedfrom=MSDN
            cpuRatio = cpuLimit.CpuRate / CpuCycles;
        }

        var systemInfoValue = systemInfo.GetSystemInfo();

        // Multiply the cpu ratio by the number of processors to get you the portion
        // of processors used from the system.
        return cpuRatio * systemInfoValue.NumberOfProcessors;
    }

    /// <summary>
    /// Gets memory limit of the system.
    /// </summary>
    /// <returns>Memory limit allocated to the system in bytes.</returns>
    private ulong GetMemoryLimits(IJobHandle jobHandle)
    {
        var memoryLimitInBytes = jobHandle.GetExtendedLimitInfo().JobMemoryLimit.ToUInt64();

        if (memoryLimitInBytes <= 0)
        {
            var memoryStatus = _memoryStatus.Value;

            // Technically, the unconstrained limit is memoryStatus.TotalPageFile.
            // Leaving this at physical as it is more understandable to consumers.
            memoryLimitInBytes = memoryStatus.TotalPhys;
        }

        return memoryLimitInBytes;
    }

    /// <summary>
    /// Gets memory usage within the system.
    /// </summary>
    /// <returns>Memory usage within the system in bytes.</returns>
    private ulong GetMemoryUsage() => _processInfo.GetMemoryUsage();

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

        var currentMemoryUsage = GetMemoryUsage();
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

        using var jobHandle = _createJobHandleObject();
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();
        var currentCpuTicks = basicAccountingInfo.TotalKernelTime + basicAccountingInfo.TotalUserTime;

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
                    _cpuPercentage = Math.Min(Hundred, usageTickDelta / timeTickDelta * Hundred); // Don't change calculation order, otherwise we loose some precision
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                }
            }

            return _cpuPercentage;
        }
    }
}

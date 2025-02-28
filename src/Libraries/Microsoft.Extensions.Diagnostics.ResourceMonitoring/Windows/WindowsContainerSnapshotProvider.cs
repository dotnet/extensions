// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Threading;
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

    private readonly Lazy<MEMORYSTATUSEX> _memoryStatus;

    /// <summary>
    /// This represents a factory method for creating the JobHandle.
    /// </summary>
    private readonly Func<IJobHandle> _createJobHandleObject;

    private readonly object _cpuLocker = new();
    private readonly object _memoryLocker = new();
    private readonly TimeProvider _timeProvider;
    private readonly IProcessInfo _processInfo;
    private readonly ILogger<WindowsContainerSnapshotProvider> _logger;
    private readonly double _memoryLimit;
    private readonly double _cpuLimit;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    public WindowsContainerSnapshotProvider(
        ILogger<WindowsContainerSnapshotProvider>? logger,
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
        ILogger<WindowsContainerSnapshotProvider>? logger,
        IMeterFactory meterFactory,
        Func<IJobHandle> createJobHandleObject,
        TimeProvider timeProvider,
        ResourceMonitoringOptions options)
    {
        _logger = logger ?? NullLogger<WindowsContainerSnapshotProvider>.Instance;
        Log.RunningInsideJobObject(_logger);

        _metricValueMultiplier = options.UseZeroToOneRangeForMetrics ? One : Hundred;

        _memoryStatus = new Lazy<MEMORYSTATUSEX>(
            memoryInfo.GetMemoryStatus,
            LazyThreadSafetyMode.ExecutionAndPublication);
        _createJobHandleObject = createJobHandleObject;
        _processInfo = processInfo;

        _timeProvider = timeProvider;

        using var jobHandle = _createJobHandleObject();

        var memoryLimitLong = GetMemoryLimit(jobHandle);
        _memoryLimit = memoryLimitLong;
        _cpuLimit = GetCpuLimit(jobHandle, systemInfo);

        // CPU request (aka guaranteed CPU units) is not supported on Windows, so we set it to the same value as CPU limit (aka maximum CPU units).
        // Memory request (aka guaranteed memory) is not supported on Windows, so we set it to the same value as memory limit (aka maximum memory).
        var cpuRequest = _cpuLimit;
        var memoryRequest = memoryLimitLong;
        Resources = new SystemResources(cpuRequest, _cpuLimit, memoryRequest, memoryLimitLong);
        Log.SystemResourcesInfo(_logger, _cpuLimit, cpuRequest, memoryLimitLong, memoryRequest);

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
        var meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        // Container based metrics:
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerCpuLimitUtilization, observeValue: CpuPercentage);
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, observeValue: () => MemoryPercentage(() => _processInfo.GetMemoryUsage()));

        // Process based metrics:
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessCpuUtilization, observeValue: CpuPercentage);
        _ = meter.CreateObservableGauge(name: ResourceUtilizationInstruments.ProcessMemoryUtilization, observeValue: () => MemoryPercentage(() => _processInfo.GetCurrentProcessMemoryUsage()));
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

    private static double GetCpuLimit(IJobHandle jobHandle, ISystemInfo systemInfo)
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
    private ulong GetMemoryLimit(IJobHandle jobHandle)
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

    private double MemoryPercentage(Func<ulong> getMemoryUsage)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_memoryLocker)
        {
            if (now < _refreshAfterMemory)
            {
                return _memoryPercentage;
            }
        }

        var memoryUsage = getMemoryUsage();

        lock (_memoryLocker)
        {
            if (now >= _refreshAfterMemory)
            {
                // Don't change calculation order, otherwise we loose some precision:
                _memoryPercentage = Math.Min(_metricValueMultiplier, memoryUsage / _memoryLimit * _metricValueMultiplier);
                _refreshAfterMemory = now.Add(_memoryRefreshInterval);
            }

            Log.MemoryUsageData(_logger, memoryUsage, _memoryLimit, _memoryPercentage);

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
                var timeTickDelta = (now.Ticks - _oldCpuTimeTicks) * _cpuLimit;
                if (usageTickDelta > 0 && timeTickDelta > 0)
                {
                    // Don't change calculation order, otherwise precision is lost:
                    _cpuPercentage = Math.Min(_metricValueMultiplier, usageTickDelta / timeTickDelta * _metricValueMultiplier);

                    Log.CpuContainerUsageData(
                        _logger, basicAccountingInfo.TotalKernelTime, basicAccountingInfo.TotalUserTime, _oldCpuUsageTicks, timeTickDelta, _cpuLimit, _cpuPercentage);

                    _oldCpuUsageTicks = currentCpuTicks;
                    _oldCpuTimeTicks = now.Ticks;
                    _refreshAfterCpu = now.Add(_cpuRefreshInterval);
                }
            }

            return _cpuPercentage;
        }
    }
}

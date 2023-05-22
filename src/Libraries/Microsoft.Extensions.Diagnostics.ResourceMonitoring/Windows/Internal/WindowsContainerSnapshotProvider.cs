// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// A data source acquiring data from the kernel.
/// </summary>
internal sealed class WindowsContainerSnapshotProvider : ISnapshotProvider
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    /// <summary>
    /// The memory status.
    /// </summary>
    private readonly Lazy<MEMORYSTATUSEX> _memoryStatus;

    /// <summary>
    /// This represents a factory method for creating the JobHandle.
    /// </summary>
    private readonly Func<IJobHandle> _createJobHandleObject;

    private readonly IProcessInfo _processInfo;

    public SystemResources Resources { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public WindowsContainerSnapshotProvider(ILogger<WindowsContainerSnapshotProvider> logger)
    {
        Log.RunningInsideJobObject(logger);

        _memoryStatus = new Lazy<MEMORYSTATUSEX>(
            new MemoryInfo().GetMemoryStatus,
            LazyThreadSafetyMode.ExecutionAndPublication);

        Lazy<SYSTEM_INFO> systemInfo = new Lazy<SYSTEM_INFO>(
            new SystemInfo().GetSystemInfo,
            LazyThreadSafetyMode.ExecutionAndPublication);

        _createJobHandleObject = CreateJobHandle;

        _processInfo = new ProcessInfoWrapper();

        // initialize system resources information
        using var jobHandle = _createJobHandleObject();

        var cpuUnits = GetGuaranteedCpuUnits(jobHandle, systemInfo);
        var memory = GetMemoryLimits(jobHandle);

        Resources = new SystemResources(cpuUnits, cpuUnits, memory, memory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsContainerSnapshotProvider"/> class.
    /// </summary>
    /// <param name="memoryInfo">A wrapper for the memory information retrieval object.</param>
    /// <param name="systemInfoObject">A wrapper for the system information retrieval object.</param>
    /// <param name="processInfo">A wrapper for the process info retrieval object.</param>
    /// <param name="createJobHandleObject">A factory method that creates <see cref="IJobHandle"/> object.</param>
    /// <remarks>This constructor enables the mocking the <see cref="WindowsContainerSnapshotProvider"/> dependencies for the purpose of Unit Testing only.</remarks>
    internal WindowsContainerSnapshotProvider(IMemoryInfo memoryInfo, ISystemInfo systemInfoObject, IProcessInfo processInfo, Func<IJobHandle> createJobHandleObject)
    {
        _memoryStatus = new Lazy<MEMORYSTATUSEX>(memoryInfo.GetMemoryStatus, LazyThreadSafetyMode.ExecutionAndPublication);
        Lazy<SYSTEM_INFO> systemInfo = new Lazy<SYSTEM_INFO>(systemInfoObject.GetSystemInfo, LazyThreadSafetyMode.ExecutionAndPublication);
        _processInfo = processInfo;
        _createJobHandleObject = createJobHandleObject;

        // initialize system resources information
        using var jobHandle = _createJobHandleObject();

        var cpuUnits = GetGuaranteedCpuUnits(jobHandle, systemInfo);
        var memory = GetMemoryLimits(jobHandle);

        Resources = new SystemResources(cpuUnits, cpuUnits, memory, memory);
    }

    public ResourceUtilizationSnapshot GetSnapshot()
    {
        // Gather the information
        // Cpu kernel and user ticks
        using var jobHandle = _createJobHandleObject();
        var basicAccountingInfo = jobHandle.GetBasicAccountingInfo();

        return new ResourceUtilizationSnapshot(
            TimeSpan.FromTicks(TimeProvider.GetUtcNow().Ticks),
            TimeSpan.FromTicks(basicAccountingInfo.TotalKernelTime),
            TimeSpan.FromTicks(basicAccountingInfo.TotalUserTime),
            GetMemoryUsage());
    }

    private static double GetGuaranteedCpuUnits(IJobHandle jobHandle, Lazy<SYSTEM_INFO> systemInfo)
    {
        // Note: This function convert the CpuRate from CPU cycles to CPU units, also it scales
        // the CPU units with the number of processors (cores) available in the system.
        const double CpuCycles = 10_000U;

        var cpuLimit = jobHandle.GetJobCpuLimitInfo();
        double cpuRatio = 1.0;
        if (((cpuLimit.ControlFlags & (uint)JobObjectInfo.JobCpuRateControlLimit.CpuRateControlEnable) != 0) &&
            (cpuLimit.ControlFlags & (uint)JobObjectInfo.JobCpuRateControlLimit.CpuRateControlHardCap) != 0)
        {
            // The CpuRate is represented as number of cycles during scheduling interval, where
            // a full cpu cycles number would equal 10_000, so for example if the CpuRate is 2_000,
            // that means that the application (or container) is assigned 20% of the total CPU available.
            // So, here we divide the CpuRate by 10_000 to convert it to a ratio (ex: 0.2 for 20% CPU).
            // For more info: https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_cpu_rate_control_information?redirectedfrom=MSDN
            cpuRatio = cpuLimit.CpuRate / CpuCycles;
        }

        // Multiply the cpu ratio by the number of processors to get you the portion
        // of processors used from the system.
        return cpuRatio * systemInfo.Value.NumberOfProcessors;
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
            // Leaving this at physical as it is more understandable to R9 consumers at present.
            memoryLimitInBytes = memoryStatus.TotalPhys;
        }

        return memoryLimitInBytes;
    }

    /// <summary>
    /// Gets memory usage within the system.
    /// </summary>
    /// <returns>Memory usage within the system in bytes.</returns>
    private ulong GetMemoryUsage()
    {
        var memoryInfo = _processInfo.GetCurrentAppMemoryInfo();

        return memoryInfo.TotalCommitUsage;
    }

    [ExcludeFromCodeCoverage]
    private JobHandleWrapper CreateJobHandle()
    {
        return new JobHandleWrapper();
    }
}

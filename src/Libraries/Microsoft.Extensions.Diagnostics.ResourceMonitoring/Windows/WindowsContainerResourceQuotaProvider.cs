// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal class WindowsContainerResourceQuotaProvider : IResourceQuotaProvider
{
    private readonly ISystemInfo _systemInfo;
    private readonly Lazy<MEMORYSTATUSEX> _memoryStatus;
    private Func<IJobHandle> _createJobHandleObject;

    public WindowsContainerResourceQuotaProvider()
        : this(new MemoryInfo(), new SystemInfo(), static () => new JobHandleWrapper())
    {
    }

    public WindowsContainerResourceQuotaProvider(IMemoryInfo memoryInfo, ISystemInfo systemInfo, Func<IJobHandle> createJobHandleObject)
    {
        _systemInfo = systemInfo;
        _createJobHandleObject = createJobHandleObject;

        _memoryStatus = new Lazy<MEMORYSTATUSEX>(
            memoryInfo.GetMemoryStatus,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ResourceQuota GetResourceQuota()
    {
        // bring logic from WindowsContainerSnapshotProvider for limits and requests
        using IJobHandle jobHandle = _createJobHandleObject();

        var resourceQuota = new ResourceQuota
        {
            LimitsCpu = GetCpuLimit(jobHandle, _systemInfo),
            LimitsMemory = GetMemoryLimit(jobHandle),
        };

        // CPU request (aka guaranteed CPU units) is not supported on Windows, so we set it to the same value as CPU limit (aka maximum CPU units).
        // Memory request (aka guaranteed memory) is not supported on Windows, so we set it to the same value as memory limit (aka maximum memory).
        resourceQuota.RequestsCpu = resourceQuota.LimitsCpu;
        resourceQuota.RequestsMemory = resourceQuota.LimitsMemory;

        return resourceQuota;
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

        SYSTEM_INFO systemInfoValue = systemInfo.GetSystemInfo();

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
            MEMORYSTATUSEX memoryStatus = _memoryStatus.Value;

            // Technically, the unconstrained limit is memoryStatus.TotalPageFile.
            // Leaving this at physical as it is more understandable to consumers.
            memoryLimitInBytes = memoryStatus.TotalPhys;
        }

        return memoryLimitInBytes;
    }
}


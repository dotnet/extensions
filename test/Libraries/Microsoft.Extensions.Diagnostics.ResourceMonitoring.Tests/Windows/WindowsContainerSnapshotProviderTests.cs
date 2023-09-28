// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Moq;
using Xunit;
using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop.JobObjectInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public sealed class WindowsContainerSnapshotProviderTests
{
    [Theory]
    [InlineData(7_000, 1U, 0.7)]
    [InlineData(10_000, 1U, 1.0)]
    [InlineData(10_000, 2U, 2.0)]
    [InlineData(5_000, 2U, 1.0)]
    public void Resources_GetsCorrectSystemResourcesValues(uint cpuRate, uint numberOfProcessors, double expectedCpuUnits)
    {
        MEMORYSTATUSEX memStatus = default;
        memStatus.TotalPhys = 3000UL;

        SYSTEM_INFO sysInfo = default;
        sysInfo.NumberOfProcessors = numberOfProcessors;

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuLimit = default;

        // This is customized to force the private method GetGuaranteedCpuUnits
        // to use the value of  CpuRate and divide it by 10_000.
        cpuLimit.ControlFlags = 5;

        // The CpuRate is the Cpu percentage multiplied by 100, check this:
        // https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_cpu_rate_control_information
        cpuLimit.CpuRate = cpuRate;

        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accountingInfo = default;
        accountingInfo.TotalKernelTime = 1000;
        accountingInfo.TotalUserTime = 1000;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
        limitInfo.JobMemoryLimit = new UIntPtr(2000);

        ProcessInfo.APP_MEMORY_INFORMATION appMemoryInfo = default;
        appMemoryInfo.TotalCommitUsage = 1000UL;

        var memoryInfoMock = new Mock<IMemoryInfo>();
        memoryInfoMock.Setup(m => m.GetMemoryStatus()).Returns(memStatus);

        var systemInfoMock = new Mock<ISystemInfo>();
        systemInfoMock.Setup(s => s.GetSystemInfo()).Returns(sysInfo);

        var processInfoMock = new Mock<IProcessInfo>();
        processInfoMock.Setup(p => p.GetCurrentAppMemoryInfo()).Returns(appMemoryInfo);

        var jobHandleMock = new Mock<IJobHandle>();
        jobHandleMock.Setup(j => j.GetJobCpuLimitInfo()).Returns(cpuLimit);
        jobHandleMock.Setup(j => j.GetBasicAccountingInfo()).Returns(accountingInfo);
        jobHandleMock.Setup(j => j.GetExtendedLimitInfo()).Returns(limitInfo);

        var provider = new WindowsContainerSnapshotProvider(
            memoryInfoMock.Object,
            systemInfoMock.Object,
            processInfoMock.Object,
            () => jobHandleMock.Object);

        Assert.Equal(expectedCpuUnits, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(expectedCpuUnits, provider.Resources.MaximumCpuUnits);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), provider.Resources.MaximumMemoryInBytes);
    }

    [Fact]
    public void GetSnapshot_ProducesCorrectSnapshot()
    {
        MEMORYSTATUSEX memStatus = default;
        memStatus.TotalPhys = 3000UL;

        SYSTEM_INFO sysInfo = default;
        sysInfo.NumberOfProcessors = 1;

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuLimit = default;

        // The ControlFlags is customized to force the private method GetGuaranteedCpuUnits
        // to not use the value of CpuRate in the calculation.
        cpuLimit.ControlFlags = 1;
        cpuLimit.CpuRate = 7_000;

        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accountingInfo = default;
        accountingInfo.TotalKernelTime = 1000;
        accountingInfo.TotalUserTime = 1000;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
        limitInfo.JobMemoryLimit = new UIntPtr(2000);

        ProcessInfo.APP_MEMORY_INFORMATION appMemoryInfo = default;
        appMemoryInfo.TotalCommitUsage = 1000UL;

        var memoryInfoMock = new Mock<IMemoryInfo>();
        memoryInfoMock.Setup(m => m.GetMemoryStatus()).Returns(memStatus);

        var systemInfoMock = new Mock<ISystemInfo>();
        systemInfoMock.Setup(s => s.GetSystemInfo()).Returns(sysInfo);

        var processInfoMock = new Mock<IProcessInfo>();
        processInfoMock.Setup(p => p.GetCurrentAppMemoryInfo()).Returns(appMemoryInfo);

        var jobHandleMock = new Mock<IJobHandle>();
        jobHandleMock.Setup(j => j.GetJobCpuLimitInfo()).Returns(cpuLimit);
        jobHandleMock.Setup(j => j.GetBasicAccountingInfo()).Returns(accountingInfo);
        jobHandleMock.Setup(j => j.GetExtendedLimitInfo()).Returns(limitInfo);

        var source = new WindowsContainerSnapshotProvider(
            memoryInfoMock.Object,
            systemInfoMock.Object,
            processInfoMock.Object,
            () => jobHandleMock.Object);
        var data = source.GetSnapshot();
        Assert.Equal(accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.MaximumMemoryInBytes);
        Assert.Equal(appMemoryInfo.TotalCommitUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }

    [Fact]
    public void GetSnapshot_ProducesCorrectSnapshotForDifferentCpuRate()
    {
        MEMORYSTATUSEX memStatus = default;
        memStatus.TotalPhys = 3000UL;

        SYSTEM_INFO sysInfo = default;
        sysInfo.NumberOfProcessors = 1;

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuLimit = default;
        cpuLimit.ControlFlags = uint.MaxValue; // force all bits in ControlFlags to be 1.
        cpuLimit.CpuRate = 7_000;

        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accountingInfo = default;
        accountingInfo.TotalKernelTime = 1000;
        accountingInfo.TotalUserTime = 1000;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
        limitInfo.JobMemoryLimit = new UIntPtr(2000);

        ProcessInfo.APP_MEMORY_INFORMATION appMemoryInfo = default;
        appMemoryInfo.TotalCommitUsage = 1000UL;

        var memoryInfoMock = new Mock<IMemoryInfo>();
        memoryInfoMock.Setup(m => m.GetMemoryStatus()).Returns(memStatus);

        var systemInfoMock = new Mock<ISystemInfo>();
        systemInfoMock.Setup(s => s.GetSystemInfo()).Returns(sysInfo);

        var processInfoMock = new Mock<IProcessInfo>();
        processInfoMock.Setup(p => p.GetCurrentAppMemoryInfo()).Returns(appMemoryInfo);

        var jobHandleMock = new Mock<IJobHandle>();
        jobHandleMock.Setup(j => j.GetJobCpuLimitInfo()).Returns(cpuLimit);
        jobHandleMock.Setup(j => j.GetBasicAccountingInfo()).Returns(accountingInfo);
        jobHandleMock.Setup(j => j.GetExtendedLimitInfo()).Returns(limitInfo);

        var source = new WindowsContainerSnapshotProvider(
            memoryInfoMock.Object,
            systemInfoMock.Object,
            processInfoMock.Object,
            () => jobHandleMock.Object);
        var data = source.GetSnapshot();

        Assert.Equal(accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(0.7, source.Resources.GuaranteedCpuUnits);
        Assert.Equal(0.7, source.Resources.MaximumCpuUnits);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.MaximumMemoryInBytes);
        Assert.Equal(appMemoryInfo.TotalCommitUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }

    [Fact]
    public void GetSnapshot_With_JobMemoryLimit_Set_To_Zero_ProducesCorrectSnapshot()
    {
        MEMORYSTATUSEX memStatus = default;
        memStatus.TotalPhys = 3000UL;

        SYSTEM_INFO sysInfo = default;
        sysInfo.NumberOfProcessors = 1;

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuLimit = default;

        // This is customized to force the private method GetGuaranteedCpuUnits
        // to set the GuaranteedCpuUnits and MaximumCpuUnits to 1.0.
        cpuLimit.ControlFlags = 4;
        cpuLimit.CpuRate = 7_000;

        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accountingInfo = default;
        accountingInfo.TotalKernelTime = 1000;
        accountingInfo.TotalUserTime = 1000;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
        limitInfo.JobMemoryLimit = new UIntPtr(0);

        ProcessInfo.APP_MEMORY_INFORMATION appMemoryInfo = default;
        appMemoryInfo.TotalCommitUsage = 3000UL;

        var memoryInfoMock = new Mock<IMemoryInfo>();
        memoryInfoMock.Setup(m => m.GetMemoryStatus()).Returns(memStatus);

        var systemInfoMock = new Mock<ISystemInfo>();
        systemInfoMock.Setup(s => s.GetSystemInfo()).Returns(sysInfo);

        var processInfoMock = new Mock<IProcessInfo>();
        processInfoMock.Setup(p => p.GetCurrentAppMemoryInfo()).Returns(appMemoryInfo);

        var jobHandleMock = new Mock<IJobHandle>();
        jobHandleMock.Setup(j => j.GetJobCpuLimitInfo()).Returns(cpuLimit);
        jobHandleMock.Setup(j => j.GetBasicAccountingInfo()).Returns(accountingInfo);
        jobHandleMock.Setup(j => j.GetExtendedLimitInfo()).Returns(limitInfo);

        var source = new WindowsContainerSnapshotProvider(
            memoryInfoMock.Object,
            systemInfoMock.Object,
            processInfoMock.Object,
            () => jobHandleMock.Object);
        var data = source.GetSnapshot();
        Assert.Equal(accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(1.0, source.Resources.GuaranteedCpuUnits);
        Assert.Equal(1.0, source.Resources.MaximumCpuUnits);
        Assert.Equal(memStatus.TotalPhys, source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(memStatus.TotalPhys, source.Resources.MaximumMemoryInBytes);
        Assert.Equal(appMemoryInfo.TotalCommitUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }
}

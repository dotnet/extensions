// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Instruments;
using Moq;
using Xunit;
using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop.JobObjectInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Windows;

public class AcceptanceTest
{
    [Fact]
    public async Task WindowsContainerSnapshotProvider_MeasuredWithKubernetesMetadata()
    {
        const ulong LimitsMemory = 2_000;
        const ulong LimitsCpu = 2_000; // 2 cores in millicores
        const ulong RequestsMemory = 1_000;
        const ulong RequestsCpu = 1_000; // 1 core in millicores

        using var environmentSetup = new TestKubernetesEnvironmentSetup();

        environmentSetup.SetupKubernetesEnvironment(
            "ACCEPTANCE_TEST_WINDOWS_",
            LimitsMemory,
            LimitsCpu,
            RequestsMemory,
            RequestsCpu);

        var logger = new FakeLogger<WindowsContainerSnapshotProvider>();
        var memoryInfoMock = new Mock<IMemoryInfo>();
        var systemInfoMock = new Mock<ISystemInfo>();
        var jobHandleMock = new Mock<IJobHandle>();
        var processInfoMock = new Mock<IProcessInfo>();

        var memStatus = new MEMORYSTATUSEX { TotalPhys = 3000UL };
        memoryInfoMock.Setup(m => m.GetMemoryStatus()).Returns(() => memStatus);

        var sysInfo = new SYSTEM_INFO { NumberOfProcessors = 2 };
        systemInfoMock.Setup(s => s.GetSystemInfo()).Returns(() => sysInfo);

        // Setup CPU accounting info with initial and updated values
        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION initialAccountingInfo = new()
        {
            TotalKernelTime = 1000,
            TotalUserTime = 1000
        };

        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION updatedAccountingInfo = new()
        {
            TotalKernelTime = 1500, // 500 ticks increase
            TotalUserTime = 1500    // 500 ticks increase
        };

        var accountingCallCount = 0;
        jobHandleMock.Setup(j => j.GetBasicAccountingInfo())
            .Returns(() =>
            {
                accountingCallCount++;
                return accountingCallCount <= 1 ? initialAccountingInfo : updatedAccountingInfo;
            });

        var cpuLimit = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION { CpuRate = 10_000 };
        jobHandleMock.Setup(j => j.GetJobCpuLimitInfo()).Returns(() => cpuLimit);

        var limitInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            JobMemoryLimit = new UIntPtr(LimitsMemory)
        };
        jobHandleMock.Setup(j => j.GetExtendedLimitInfo()).Returns(() => limitInfo);

        ulong initialAppMemoryUsage = 200UL;
        ulong initialContainerMemoryUsage = 400UL;
        ulong updatedAppMemoryUsage = 600UL;
        ulong updatedContainerMemoryUsage = 1200UL;

        var processMemoryCallCount = 0;
        processInfoMock.Setup(p => p.GetCurrentProcessMemoryUsage())
            .Returns(() =>
            {
                processMemoryCallCount++;
                return processMemoryCallCount <= 1 ? initialAppMemoryUsage : updatedAppMemoryUsage;
            });

        var containerMemoryCallCount = 0;
        processInfoMock.Setup(p => p.GetMemoryUsage())
            .Returns(() =>
            {
                containerMemoryCallCount++;
                return containerMemoryCallCount <= 1 ? initialContainerMemoryUsage : updatedContainerMemoryUsage;
            });

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(WindowsContainerSnapshotProvider_MeasuredWithKubernetesMetadata));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        using var containerCpuLimitMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerCpuLimitUtilization, fakeClock);
        using var containerMemoryLimitMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, fakeClock);
        using var containerCpuRequestMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerCpuRequestUtilization, fakeClock);
        using var containerMemoryRequestMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerMemoryRequestUtilization, fakeClock);

        var options = new ResourceMonitoringOptions
        {
            MemoryConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2),
            CpuConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2),
            UseZeroToOneRangeForMetrics = false
        };

        var kubernetesMetadata = new KubernetesMetadata
        {
            LimitsMemory = LimitsMemory,
            LimitsCpu = LimitsCpu,
            RequestsMemory = RequestsMemory,
            RequestsCpu = RequestsCpu
        };
        var kubernetesResourceQuotaProvider = new KubernetesResourceQuotaProvider(kubernetesMetadata);

        var snapshotProvider = new WindowsContainerSnapshotProvider(
            processInfoMock.Object,
            logger,
            meterFactoryMock.Object,
            () => jobHandleMock.Object,
            fakeClock,
            options,
            kubernetesResourceQuotaProvider);

        // Initial state
        containerCpuLimitMetricCollector.RecordObservableInstruments();
        containerMemoryLimitMetricCollector.RecordObservableInstruments();
        containerCpuRequestMetricCollector.RecordObservableInstruments();
        containerMemoryRequestMetricCollector.RecordObservableInstruments();

        Assert.NotNull(containerCpuLimitMetricCollector.LastMeasurement?.Value);
        Assert.NotNull(containerMemoryLimitMetricCollector.LastMeasurement?.Value);
        Assert.NotNull(containerCpuRequestMetricCollector.LastMeasurement?.Value);
        Assert.NotNull(containerMemoryRequestMetricCollector.LastMeasurement?.Value);

        // Initial CPU metrics should be NaN as no time has passed for calculation
        Assert.True(double.IsNaN(containerCpuLimitMetricCollector.LastMeasurement.Value));
        Assert.True(double.IsNaN(containerCpuRequestMetricCollector.LastMeasurement.Value));

        // Container memory: 400 bytes / 2000 bytes limit = 20%
        Assert.Equal(20, containerMemoryLimitMetricCollector.LastMeasurement.Value);

        // Container memory: 400 bytes / 1000 bytes request = 40%
        Assert.Equal(40, containerMemoryRequestMetricCollector.LastMeasurement.Value);

        // Advance time to trigger refresh
        fakeClock.Advance(options.MemoryConsumptionRefreshInterval);

        containerCpuLimitMetricCollector.RecordObservableInstruments();
        containerMemoryLimitMetricCollector.RecordObservableInstruments();
        containerCpuRequestMetricCollector.RecordObservableInstruments();
        containerMemoryRequestMetricCollector.RecordObservableInstruments();

        // CPU: 1000 ticks increase over 2ms = 2.5% utilization against 2 core limit
        Assert.Equal(2.5, containerCpuLimitMetricCollector.LastMeasurement.Value);

        // CPU: 2.5% of 2 cores against 1 core request = 5%
        Assert.Equal(5, containerCpuRequestMetricCollector.LastMeasurement.Value);

        // Container memory: 1200 bytes / 2000 bytes limit = 60%
        Assert.Equal(60, containerMemoryLimitMetricCollector.LastMeasurement.Value);

        // Container memory: 1200 bytes / 1000 bytes request = 120%, but Min() to 100%
        Assert.Equal(100, containerMemoryRequestMetricCollector.LastMeasurement.Value);

        await Task.CompletedTask;
    }
}

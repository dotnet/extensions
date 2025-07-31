// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Instruments;
using Moq;
using VerifyXunit;
using Xunit;
using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop.JobObjectInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public sealed class WindowsContainerSnapshotProviderTests
{
    private const string VerifiedDataDirectory = "Verified";

    private readonly FakeLogger<WindowsContainerSnapshotProvider> _logger;
    private readonly MEMORYSTATUSEX _memStatus;

    private readonly Mock<IMeterFactory> _meterFactory;
    private readonly Mock<IMemoryInfo> _memoryInfoMock = new();
    private readonly Mock<ISystemInfo> _systemInfoMock = new();
    private readonly Mock<IJobHandle> _jobHandleMock = new();
    private readonly Mock<IProcessInfo> _processInfoMock = new();

    private SYSTEM_INFO _sysInfo;
    private JOBOBJECT_BASIC_ACCOUNTING_INFORMATION _accountingInfo;
    private JOBOBJECT_CPU_RATE_CONTROL_INFORMATION _cpuLimit;
    private ulong _appMemoryUsage;
    private JOBOBJECT_EXTENDED_LIMIT_INFORMATION _limitInfo;

    public WindowsContainerSnapshotProviderTests()
    {
        using var meter = new Meter(nameof(WindowsContainerSnapshotProvider));
        _meterFactory = new Mock<IMeterFactory>();
        _meterFactory.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        _logger = new FakeLogger<WindowsContainerSnapshotProvider>();

        _memStatus.TotalPhys = 3000UL;
        _memoryInfoMock.Setup(m => m.GetMemoryStatus())
            .Returns(() => _memStatus);

        _sysInfo.NumberOfProcessors = 1;
        _systemInfoMock.Setup(s => s.GetSystemInfo())
            .Returns(() => _sysInfo);

        _accountingInfo.TotalKernelTime = 1000;
        _accountingInfo.TotalUserTime = 1000;
        _jobHandleMock.Setup(j => j.GetBasicAccountingInfo())
            .Returns(() => _accountingInfo);

        _cpuLimit.CpuRate = 7_000;
        _jobHandleMock.Setup(j => j.GetJobCpuLimitInfo())
            .Returns(() => _cpuLimit);

        _limitInfo.JobMemoryLimit = new UIntPtr(2000);
        _jobHandleMock.Setup(j => j.GetExtendedLimitInfo())
            .Returns(() => _limitInfo);

        _appMemoryUsage = 1000UL;
        _processInfoMock.Setup(p => p.GetCurrentProcessMemoryUsage())
            .Returns(() => _appMemoryUsage);
    }

    [Theory]
    [InlineData(7_000, 1U, 0.7)]
    [InlineData(10_000, 1U, 1.0)]
    [InlineData(10_000, 2U, 2.0)]
    [InlineData(5_000, 2U, 1.0)]
    public void Resources_GetsCorrectSystemResourcesValues(uint cpuRate, uint numberOfProcessors, double expectedCpuUnits)
    {
        _sysInfo.NumberOfProcessors = numberOfProcessors;

        // This is customized to force the private method GetGuaranteedCpuUnits
        // to use the value of CpuRate and divide it by 10_000.
        _cpuLimit.ControlFlags = 5;

        // The CpuRate is the Cpu percentage multiplied by 100, check this:
        // https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_cpu_rate_control_information
        _cpuLimit.CpuRate = cpuRate;

        var provider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            _meterFactory.Object,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        Assert.Equal(expectedCpuUnits, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(expectedCpuUnits, provider.Resources.MaximumCpuUnits);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), provider.Resources.MaximumMemoryInBytes);
    }

    [Fact]
    public void GetSnapshot_ProducesCorrectSnapshot()
    {
        // The ControlFlags is customized to force the private method GetGuaranteedCpuUnits
        // to not use the value of CpuRate in the calculation.
        _cpuLimit.ControlFlags = 1;

        var source = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            _meterFactory.Object,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        var data = source.GetSnapshot();
        Assert.Equal(_accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(_accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.MaximumMemoryInBytes);
        Assert.Equal(_appMemoryUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }

    [Fact]
    public void GetSnapshot_ProducesCorrectSnapshotForDifferentCpuRate()
    {
        _cpuLimit.ControlFlags = uint.MaxValue; // force all bits in ControlFlags to be 1.

        var source = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            _meterFactory.Object,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        var data = source.GetSnapshot();

        Assert.Equal(_accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(_accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(0.7, source.Resources.GuaranteedCpuUnits);
        Assert.Equal(0.7, source.Resources.MaximumCpuUnits);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(_limitInfo.JobMemoryLimit.ToUInt64(), source.Resources.MaximumMemoryInBytes);
        Assert.Equal(_appMemoryUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }

    [Fact]
    public void GetSnapshot_With_JobMemoryLimit_Set_To_Zero_ProducesCorrectSnapshot()
    {
        // This is customized to force the private method GetGuaranteedCpuUnits
        // to set the GuaranteedCpuUnits and MaximumCpuUnits to 1.0.
        _cpuLimit.ControlFlags = 4;

        _limitInfo.JobMemoryLimit = new UIntPtr(0);

        _appMemoryUsage = 3000UL;

        var source = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            _meterFactory.Object,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        var data = source.GetSnapshot();
        Assert.Equal(_accountingInfo.TotalKernelTime, data.KernelTimeSinceStart.Ticks);
        Assert.Equal(_accountingInfo.TotalUserTime, data.UserTimeSinceStart.Ticks);
        Assert.Equal(1.0, source.Resources.GuaranteedCpuUnits);
        Assert.Equal(1.0, source.Resources.MaximumCpuUnits);
        Assert.Equal(_memStatus.TotalPhys, source.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(_memStatus.TotalPhys, source.Resources.MaximumMemoryInBytes);
        Assert.Equal(_appMemoryUsage, data.MemoryUsageInBytes);
        Assert.True(data.MemoryUsageInBytes > 0);
    }

    [Fact]
    public void SnapshotProvider_EmitsCpuTimeMetric()
    {
        // Simulating 10% CPU usage (2 CPUs, 2000 ticks initially, 4000 ticks after 1 ms):
        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION updatedAccountingInfo = default;
        updatedAccountingInfo.TotalKernelTime = 2500;
        updatedAccountingInfo.TotalUserTime = 1500;

        _jobHandleMock.SetupSequence(j => j.GetBasicAccountingInfo())
            .Returns(_accountingInfo)
            .Returns(_accountingInfo)
            .Returns(updatedAccountingInfo)
            .Returns(updatedAccountingInfo)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        _sysInfo.NumberOfProcessors = 2;

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(SnapshotProvider_EmitsCpuMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);
        using var metricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerCpuTime, fakeClock);

        var options = new ResourceMonitoringOptions { CpuConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2) };

        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactoryMock.Object,
            () => _jobHandleMock.Object,
            fakeClock,
            options);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        var snapshot = metricCollector.GetMeasurementSnapshot();
        Assert.Equal(2, snapshot.Count);
        Assert.Contains(_accountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));
        Assert.Contains(_accountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        snapshot = metricCollector.GetMeasurementSnapshot();
        Assert.Contains(updatedAccountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));
        Assert.Contains(updatedAccountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));

        // Step #2 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        snapshot = metricCollector.GetMeasurementSnapshot();

        // CPU time should be the same as before, as we're not simulating any CPU usage:
        Assert.Contains(updatedAccountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));
        Assert.Contains(updatedAccountingInfo.TotalKernelTime / (double)TimeSpan.TicksPerSecond, snapshot.Select(m => m.Value));
    }

    [Theory]
    [InlineData(ResourceUtilizationInstruments.ProcessCpuUtilization, true)]
    [InlineData(ResourceUtilizationInstruments.ProcessCpuUtilization, false)]
    [InlineData(ResourceUtilizationInstruments.ContainerCpuLimitUtilization, true)]
    [InlineData(ResourceUtilizationInstruments.ContainerCpuLimitUtilization, false)]
    public void SnapshotProvider_EmitsCpuMetrics(string instrumentName, bool useZeroToOneRange)
    {
        // Simulating 10% CPU usage (2 CPUs, 2000 ticks initially, 4000 ticks after 1 ms):
        JOBOBJECT_BASIC_ACCOUNTING_INFORMATION updatedAccountingInfo = default;
        updatedAccountingInfo.TotalKernelTime = 2500;
        updatedAccountingInfo.TotalUserTime = 1500;

        _jobHandleMock.SetupSequence(j => j.GetBasicAccountingInfo())
            .Returns(() => _accountingInfo)
            .Returns(updatedAccountingInfo)
            .Returns(updatedAccountingInfo)
            .Returns(updatedAccountingInfo)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        _sysInfo.NumberOfProcessors = 2;

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(SnapshotProvider_EmitsCpuMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);
        using var metricCollector = new MetricCollector<double>(meter, instrumentName, fakeClock);

        var options = new ResourceMonitoringOptions
        {
            CpuConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2),
            UseZeroToOneRangeForMetrics = useZeroToOneRange
        };
        var multiplier = useZeroToOneRange ? 1 : 100;
        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactoryMock.Object,
            () => _jobHandleMock.Object,
            fakeClock,
            options);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        Assert.NotNull(metricCollector.LastMeasurement);
        Assert.True(double.IsNaN(metricCollector.LastMeasurement.Value));

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        Assert.Equal(0.1 * multiplier, metricCollector.LastMeasurement.Value); // Consumed 10% of the CPU.

        // Step #2 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        // CPU usage should be the same as before, as we didn't recalculate it:
        Assert.Equal(0.1 * multiplier, metricCollector.LastMeasurement.Value); // Still consuming 10% as gauge wasn't updated.

        // Step #3 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        // CPU usage should be the same as before, as we're not simulating any CPU usage:
        Assert.Equal(0.1 * multiplier, metricCollector.LastMeasurement.Value); // Consumed 10% of the CPU.
    }

    [Theory]
    [InlineData(ResourceUtilizationInstruments.ProcessMemoryUtilization, true)]
    [InlineData(ResourceUtilizationInstruments.ProcessMemoryUtilization, false)]
    [InlineData(ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, true)]
    [InlineData(ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, false)]
    public void SnapshotProvider_EmitsMemoryMetrics(string instrumentName, bool useZeroToOneRange)
    {
        _appMemoryUsage = 200UL;
        ulong updatedAppMemoryUsage = 600UL;

        _processInfoMock.SetupSequence(p => p.GetCurrentProcessMemoryUsage())
            .Returns(() => _appMemoryUsage)
            .Returns(updatedAppMemoryUsage)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        _processInfoMock.SetupSequence(p => p.GetMemoryUsage())
            .Returns(() => _appMemoryUsage)
            .Returns(updatedAppMemoryUsage)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(SnapshotProvider_EmitsMemoryMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);
        using var metricCollector = new MetricCollector<double>(meter, instrumentName, fakeClock);

        var options = new ResourceMonitoringOptions
        {
            MemoryConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2),
            UseZeroToOneRangeForMetrics = useZeroToOneRange
        };
        var multiplier = useZeroToOneRange ? 1 : 100;
        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactoryMock.Object,
            () => _jobHandleMock.Object,
            fakeClock,
            options);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        Assert.NotNull(metricCollector.LastMeasurement?.Value);
        Assert.Equal(0.1 * multiplier, metricCollector.LastMeasurement.Value); // Consuming 10% of the memory initially.

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(options.MemoryConsumptionRefreshInterval - TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(0.1 * multiplier, metricCollector.LastMeasurement.Value); // Still consuming 10% as gauge wasn't updated.

        // Step #2 - simulate 2 milliseconds passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(0.3 * multiplier, metricCollector.LastMeasurement.Value); // Consuming 30% of the memory afterwards.
    }

    [Fact]
    public void SnapshotProvider_TestMemoryMetricsTogether()
    {
        _appMemoryUsage = 200UL;
        ulong containerMemoryUsage = 400UL;
        ulong updatedAppMemoryUsage = 600UL;
        ulong updatedContainerMemoryUsage = 1200UL;

        _processInfoMock.SetupSequence(p => p.GetCurrentProcessMemoryUsage())
            .Returns(() => _appMemoryUsage)
            .Returns(updatedAppMemoryUsage)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        _processInfoMock.SetupSequence(p => p.GetMemoryUsage())
            .Returns(() => containerMemoryUsage)
            .Returns(updatedContainerMemoryUsage)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(SnapshotProvider_TestMemoryMetricsTogether));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);
        using var processMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ProcessMemoryUtilization, fakeClock);
        using var containerLimitMetricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.ContainerMemoryLimitUtilization, fakeClock);
        using var containerUsageMetricCollector = new MetricCollector<long>(meter, ResourceUtilizationInstruments.ContainerMemoryUsage, fakeClock);

        var options = new ResourceMonitoringOptions
        {
            MemoryConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2)
        };
        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactoryMock.Object,
            () => _jobHandleMock.Object,
            fakeClock,
            options);

        // Step #0 - state in the beginning:
        processMetricCollector.RecordObservableInstruments();
        containerLimitMetricCollector.RecordObservableInstruments();
        containerUsageMetricCollector.RecordObservableInstruments();

        Assert.NotNull(processMetricCollector.LastMeasurement?.Value);
        Assert.NotNull(containerLimitMetricCollector.LastMeasurement?.Value);
        Assert.NotNull(containerUsageMetricCollector.LastMeasurement?.Value);

        Assert.Equal(10, processMetricCollector.LastMeasurement.Value); // Process is consuming 10% of memory limit initially.
        Assert.Equal(20, containerLimitMetricCollector.LastMeasurement.Value); // The whole container is consuming 20% of the memory limit initially.
        Assert.Equal((long)containerMemoryUsage, containerUsageMetricCollector.LastMeasurement.Value); // 400 bytes of memory usage initially.

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(options.MemoryConsumptionRefreshInterval - TimeSpan.FromMilliseconds(1));

        processMetricCollector.RecordObservableInstruments();
        containerUsageMetricCollector.RecordObservableInstruments();
        containerLimitMetricCollector.RecordObservableInstruments();

        // Still consuming 10% and 20% as values weren't updated yet - not enough time passed.
        Assert.Equal(10, processMetricCollector.LastMeasurement.Value);
        Assert.Equal(20, containerLimitMetricCollector.LastMeasurement.Value);
        Assert.Equal((long)containerMemoryUsage, containerUsageMetricCollector.LastMeasurement.Value);

        // Step #2 - simulate 2 milliseconds passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));

        processMetricCollector.RecordObservableInstruments();
        containerLimitMetricCollector.RecordObservableInstruments();
        containerUsageMetricCollector.RecordObservableInstruments();

        // App is consuming 30%, and container is consuming 60% of the limit:
        Assert.Equal(30, processMetricCollector.LastMeasurement.Value);
        Assert.Equal(60, containerLimitMetricCollector.LastMeasurement.Value);
        Assert.Equal((long)updatedContainerMemoryUsage, containerUsageMetricCollector.LastMeasurement.Value);
    }

    [Fact]
    public void SnapshotProvider_EmitsMemoryUsageMetric()
    {
        _appMemoryUsage = 200UL;
        const ulong UpdatedAppMemoryUsage = 600UL;
        const ulong UpdatedAppMemoryUsage2 = 300UL;

        _processInfoMock.SetupSequence(p => p.GetCurrentProcessMemoryUsage())
            .Returns(() => _appMemoryUsage)
            .Returns(UpdatedAppMemoryUsage)
            .Returns(UpdatedAppMemoryUsage2)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        _processInfoMock.SetupSequence(p => p.GetMemoryUsage())
            .Returns(() => _appMemoryUsage)
            .Returns(UpdatedAppMemoryUsage)
            .Returns(UpdatedAppMemoryUsage2)
            .Throws(new InvalidOperationException("We shouldn't hit here..."));

        var fakeClock = new FakeTimeProvider();
        using var meter = new Meter(nameof(SnapshotProvider_EmitsMemoryMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);
        using var metricCollector = new MetricCollector<long>(meter, ResourceUtilizationInstruments.ContainerMemoryUsage, fakeClock);

        var options = new ResourceMonitoringOptions
        {
            MemoryConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2),
        };
        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactoryMock.Object,
            () => _jobHandleMock.Object,
            fakeClock,
            options);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        Assert.NotNull(metricCollector.LastMeasurement?.Value);
        Assert.Equal(200, metricCollector.LastMeasurement.Value); // Consuming 200 bytes initially.

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(options.MemoryConsumptionRefreshInterval - TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(200, metricCollector.LastMeasurement.Value); // Still consuming 200 bytes as metric wasn't updated.

        // Step #2 - simulate 2 milliseconds passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(2));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(600, metricCollector.LastMeasurement.Value); // Consuming 600 bytes.

        // Step #3 - simulate 2 milliseconds passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(2));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(300, metricCollector.LastMeasurement.Value); // Consuming 300 bytes.
    }

    [Fact]
    public Task SnapshotProvider_EmitsLogRecord()
    {
        var snapshotProvider = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            _meterFactory.Object,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        var logRecords = _logger.Collector.GetSnapshot();

        return Verifier.Verify(logRecords).UniqueForRuntime().UseDirectory(VerifiedDataDirectory);
    }

    [Fact]
    public void Provider_Creates_Meter_With_Correct_Name()
    {
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meterFactory = new TestMeterFactory();

        _ = new WindowsContainerSnapshotProvider(
            _memoryInfoMock.Object,
            _systemInfoMock.Object,
            _processInfoMock.Object,
            _logger,
            meterFactory,
            () => _jobHandleMock.Object,
            new FakeTimeProvider(),
            new());

        var meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }
}

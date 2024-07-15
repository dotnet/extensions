// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public sealed class WindowsSnapshotProviderTests
{
    private readonly Mock<IMeterFactory> _meterFactoryMock;
    private readonly FakeLogger<WindowsSnapshotProvider> _fakeLogger;
    private readonly IOptions<ResourceMonitoringOptions> _options;

    public WindowsSnapshotProviderTests()
    {
        _options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter(nameof(BasicConstructor));
        _meterFactoryMock = new Mock<IMeterFactory>();
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        _fakeLogger = new FakeLogger<WindowsSnapshotProvider>();
    }

    [ConditionalFact]
    public void BasicConstructor()
    {
        var provider = new WindowsSnapshotProvider(_fakeLogger, _meterFactoryMock.Object, _options);
        var memoryStatus = new MemoryInfo().GetMemoryStatus();

        Assert.Equal(Environment.ProcessorCount, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(Environment.ProcessorCount, provider.Resources.MaximumCpuUnits);
        Assert.Equal(memoryStatus.TotalPhys, provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(memoryStatus.TotalPhys, provider.Resources.MaximumMemoryInBytes);
    }

    [ConditionalFact]
    public void GetSnapshot_DoesNotThrowExceptions()
    {
        var provider = new WindowsSnapshotProvider(_fakeLogger, _meterFactoryMock.Object, _options);

        var exception = Record.Exception(() => provider.GetSnapshot());
        Assert.Null(exception);
    }

    [ConditionalFact]
    public void SnapshotProvider_EmitsLogRecord()
    {
        var provider = new WindowsSnapshotProvider(_fakeLogger, _meterFactoryMock.Object, _options);
        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var logRecord = Assert.Single(logRecords);
        Assert.StartsWith("Resource Monitoring is running outside of Job Object", logRecord.Message);
    }

    [ConditionalFact]
    public void SnapshotProvider_EmitsCpuMetrics()
    {
        var fakeClock = new FakeTimeProvider();
        var cpuTicks = 500L;
        var options = new ResourceMonitoringOptions { CpuConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2) };
        using var meter = new Meter(nameof(SnapshotProvider_EmitsCpuMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var snapshotProvider = new WindowsSnapshotProvider(_fakeLogger, meterFactoryMock.Object, options, fakeClock,
            static () => 2, () => cpuTicks, static () => 0L, static () => 1UL);

        cpuTicks = 1_500L;

        using var metricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.CpuUtilization, fakeClock);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        Assert.NotNull(metricCollector.LastMeasurement);
        Assert.True(double.IsNaN(metricCollector.LastMeasurement.Value));

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();
        Assert.Equal(5, metricCollector.LastMeasurement?.Value); // Consuming 5% of the CPU (2 CPUs, 1000 ticks, 1ms).

        // Step #2 - simulate another 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        // CPU usage should be the same as before, as we're not simulating any CPU usage:
        Assert.Equal(5, metricCollector.LastMeasurement?.Value); // Still consuming 5% of the CPU
    }

    [ConditionalFact]
    public void SnapshotProvider_EmitsMemoryMetrics()
    {
        var fakeClock = new FakeTimeProvider();
        long memoryUsed = 300L;
        var options = new ResourceMonitoringOptions { MemoryConsumptionRefreshInterval = TimeSpan.FromMilliseconds(2) };
        using var meter = new Meter(nameof(SnapshotProvider_EmitsMemoryMetrics));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var snapshotProvider = new WindowsSnapshotProvider(_fakeLogger, meterFactoryMock.Object, options, fakeClock, static () => 1, static () => 0, () => memoryUsed, static () => 3000UL);

        using var metricCollector = new MetricCollector<double>(meter, ResourceUtilizationInstruments.MemoryUtilization, fakeClock);

        // Step #0 - state in the beginning:
        metricCollector.RecordObservableInstruments();
        Assert.NotNull(metricCollector.LastMeasurement);
        Assert.Equal(10, metricCollector.LastMeasurement.Value); // Consuming 5% of the memory initially

        memoryUsed = 900L; // Simulate 30% memory usage.

        // Step #1 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        Assert.Equal(10, metricCollector.LastMeasurement.Value); // Still consuming 10% as gauge wasn't updated.

        // Step #2 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        metricCollector.RecordObservableInstruments();

        Assert.Equal(30, metricCollector.LastMeasurement.Value); // Consuming 30% of the memory afterwards

        memoryUsed = 3_100L; // Simulate more than 100% memory usage

        // Step #3 - simulate 1 millisecond passing and collect metrics again:
        fakeClock.Advance(options.MemoryConsumptionRefreshInterval);
        metricCollector.RecordObservableInstruments();

        // Memory usage should be the same as before, as we're not simulating any CPU usage:
        Assert.Equal(100, metricCollector.LastMeasurement.Value); // Consuming 100% of the memory
    }

    [ConditionalFact]
    public void Provider_Returns_MemoryConsumption()
    {
        // This is a synthetic test to have full test coverage:
        var usage = WindowsSnapshotProvider.GetMemoryUsageInBytes();
        Assert.InRange(usage, 0, long.MaxValue);
    }
}

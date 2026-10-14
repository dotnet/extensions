// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Instruments;
using Moq;
using VerifyXunit;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

public sealed class LinuxUtilizationProviderTests
{
    public LinuxUtilizationProviderTests()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Skipped on Windows/macOS");
    }

    private const string VerifiedDataDirectory = "Verified";

    [Fact]
    public void Provider_Registers_Instruments()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions());
        using var meter = new Meter(nameof(Provider_Registers_Instruments));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.limit_in_bytes"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "50"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "60"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "6"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "524288"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.shares"), "1024"},
        });

        var parser = new LinuxUtilizationParserCgroupV1(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, TimeProvider.System);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        Assert.Equal(7, samples.Count);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization);
        Assert.Equal(0.5, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization);
        Assert.Equal(0.5, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryUsage);
        Assert.Equal(524288, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryUsage).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(0.5, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value);
    }

    [Fact]
    public void Provider_Registers_Instruments_CgroupV2()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter(nameof(Provider_Registers_Instruments_CgroupV2));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), "usage_usec 102312\nnr_periods 50"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.max"), "20000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 312312"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "524288423423"},
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), "4"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, TimeProvider.System);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        Assert.Equal(6, samples.Count);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value);
    }

    [Fact]
    public Task Provider_EmitsLogRecord()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter(nameof(Provider_EmitsLogRecord));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), "usage_usec 102312\nnr_periods 50"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.max"), "20000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 312312"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "524288423423"},
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), "4"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var snapshotProvider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, TimeProvider.System);
        var logRecords = logger.Collector.GetSnapshot();

        return Verifier.Verify(logRecords).UseDirectory(VerifiedDataDirectory);
    }

    [Fact]
    public void Provider_Creates_Meter_With_Correct_Name()
    {
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meterFactory = new TestMeterFactory();

        var parser = new DummyLinuxUtilizationParser();
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        _ = new LinuxUtilizationProvider(options, parser, meterFactory, resourceQuotaProvider);

        var meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }

    [Fact]
    public void Provider_Registers_Instruments_CgroupV2_WithoutHostCpu()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions { UseLinuxCalculationV2 = true });
        using var meter = new Meter(nameof(Provider_Registers_Instruments_CgroupV2_WithoutHostCpu));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/fakeslice" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.stat"), "usage_usec 102312\nuser_usec 72312\nsystem_usec 30000\nnr_periods 50"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.max"), "20000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 312312"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "524288423423"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.weight"), "4"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, TimeProvider.System);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        Assert.Equal(7, samples.Count);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuTime);
        Assert.Equal(new[] { 0.072312, 0.03 }, samples.Where(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuTime).Select(i => i.value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value);
    }

    [Fact]
    public void Provider_GetMeasurementWithRetry_HandlesExceptionAndRecovers()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions());
        using var meter = new Meter(nameof(Provider_GetMeasurementWithRetry_HandlesExceptionAndRecovers));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var callCount = 0;
        var parserMock = new Mock<ILinuxUtilizationParser>();
        parserMock.Setup(p => p.GetMemoryUsageInBytes()).Returns(() =>
        {
            callCount++;
            if (callCount <= 1)
            {
                throw new FileNotFoundException("Simulated failure to read file");
            }

            return 420UL;
        });
        parserMock.Setup(p => p.GetAvailableMemoryInBytes()).Returns(1000UL);
        parserMock.Setup(p => p.GetCgroupRequestCpu()).Returns(10f);
        parserMock.Setup(p => p.GetCgroupLimitedCpus()).Returns(12f);

        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parserMock.Object, options);
        var provider = new LinuxUtilizationProvider(options, parserMock.Object, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();
        Assert.DoesNotContain(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);

        fakeTime.Advance(TimeSpan.FromMinutes(1));
        listener.RecordObservableInstruments();
        Assert.DoesNotContain(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);

        fakeTime.Advance(TimeSpan.FromMinutes(5));
        listener.RecordObservableInstruments();
        var metric = samples.SingleOrDefault(x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(0.42, metric.value);

        parserMock.Verify(p => p.GetMemoryUsageInBytes(), Times.Exactly(2));
    }

    [Fact]
    public void Provider_GetMeasurementWithRetry_UnhandledException_DoesNotBlockFutureReads()
    {
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions());
        using var meter = new Meter(nameof(Provider_GetMeasurementWithRetry_UnhandledException_DoesNotBlockFutureReads));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var callCount = 0;
        var parserMock = new Mock<ILinuxUtilizationParser>();
        parserMock.Setup(p => p.GetMemoryUsageInBytes()).Returns(() =>
        {
            // 4 represents number of calls of GetMemoryUsageInBytes() in LinuxUtilizationProvider in one interval
            callCount++;
            if (callCount <= 4)
            {
                throw new InvalidOperationException("Simulated unhandled exception");
            }

            return 1234UL;
        });
        parserMock.Setup(p => p.GetAvailableMemoryInBytes()).Returns(2000UL);
        parserMock.Setup(p => p.GetCgroupRequestCpu()).Returns(10f);
        parserMock.Setup(p => p.GetCgroupLimitedCpus()).Returns(12f);

        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parserMock.Object, options);
        var provider = new LinuxUtilizationProvider(options, parserMock.Object, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();

        Assert.Throws<AggregateException>(() => listener.RecordObservableInstruments());
        Assert.DoesNotContain(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        parserMock.Verify(p => p.GetMemoryUsageInBytes(), Times.Exactly(4));

        fakeTime.Advance(TimeSpan.FromMinutes(1));
        listener.RecordObservableInstruments();
        var metric = samples.SingleOrDefault(x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(1234f / 2000f, metric.value, 0.01f);

        parserMock.Verify(p => p.GetMemoryUsageInBytes(), Times.Exactly(5));
    }

    [Fact]
    public void Provider_WithZeroToOneRangeFalse_AndCalculationV1_ReturnsHundredBasedValues()
    {
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions
        {
            UseZeroToOneRangeForLinuxMetrics = false,
            UseLinuxCalculationV2 = false
        });

        using var meter = new Meter(nameof(Provider_WithZeroToOneRangeFalse_AndCalculationV1_ReturnsHundredBasedValues));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.limit_in_bytes"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "100000000"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "200000"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "100000"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.shares"), "1024"},
        });

        var parser = new LinuxUtilizationParserCgroupV1(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        var memoryLimit = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value;
        Assert.Equal(50.0, memoryLimit);

        var memoryRequest = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value;
        Assert.Equal(50.0, memoryRequest);

        var processMemory = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value;
        Assert.Equal(50.0, processMemory);

        var memoryUsage = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryUsage).value;
        Assert.Equal(1_048_576, memoryUsage);
    }

    [Fact]
    public void Provider_WithZeroToOneRangeTrue_AndCalculationV1_ReturnsNormalizedValues()
    {
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions
        {
            UseZeroToOneRangeForLinuxMetrics = true,
            UseLinuxCalculationV2 = false
        });

        using var meter = new Meter(nameof(Provider_WithZeroToOneRangeTrue_AndCalculationV1_ReturnsNormalizedValues));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.limit_in_bytes"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpuacct/cpuacct.usage"), "100000000"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "200000"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "100000"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.shares"), "1024"},
        });

        var parser = new LinuxUtilizationParserCgroupV1(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        var memoryLimit = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value;
        Assert.Equal(0.5, memoryLimit);

        var memoryRequest = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value;
        Assert.Equal(0.5, memoryRequest);

        var processMemory = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value;
        Assert.Equal(0.5, processMemory);

        var memoryUsage = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryUsage).value;
        Assert.Equal(1_048_576, memoryUsage);
    }

    [Fact]
    public void Provider_WithZeroToOneRangeFalse_AndCalculationV2_ReturnsHundredBasedValues()
    {
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions
        {
            UseZeroToOneRangeForLinuxMetrics = false,
            UseLinuxCalculationV2 = true
        });

        using var meter = new Meter(nameof(Provider_WithZeroToOneRangeFalse_AndCalculationV2_ReturnsHundredBasedValues));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/fakeslice" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.stat"), "usage_usec 200000\nuser_usec 150000\nsystem_usec 50000\nnr_periods 100"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.max"), "200000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.weight"), "100"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        var memoryLimit = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value;
        Assert.Equal(50.0, memoryLimit);

        var memoryRequest = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value;
        Assert.Equal(50.0, memoryRequest);

        fakeTime.Advance(TimeSpan.FromSeconds(10));
        samples.Clear();
        listener.RecordObservableInstruments();

        var cpuLimitSamples = samples.Where(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).ToList();
        if (cpuLimitSamples.Any() && !double.IsNaN(cpuLimitSamples.First().value))
        {
            Assert.True(cpuLimitSamples.First().value <= 100.0);
        }
    }

    [Fact]
    public void Provider_WithZeroToOneRangeTrue_AndCalculationV2_ReturnsNormalizedValues()
    {
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        var options = Options.Options.Create(new ResourceMonitoringOptions
        {
            UseZeroToOneRangeForLinuxMetrics = true,
            UseLinuxCalculationV2 = true
        });

        using var meter = new Meter(nameof(Provider_WithZeroToOneRangeTrue_AndCalculationV2_ReturnsNormalizedValues));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/fakeslice" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.stat"), "usage_usec 200000\nuser_usec 150000\nsystem_usec 50000\nnr_periods 100"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.max"), "200000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.weight"), "100"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, logger, fakeTime);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(Instrument instrument, double value)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (ReferenceEquals(meter, instrument.Meter))
            {
                samples.Add((instrument, value));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        var memoryLimit = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value;
        Assert.Equal(0.5, memoryLimit);

        var memoryRequest = samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryRequestUtilization).value;
        Assert.Equal(0.5, memoryRequest);

        fakeTime.Advance(TimeSpan.FromSeconds(10));
        samples.Clear();
        listener.RecordObservableInstruments();

        var cpuLimitSamples = samples.Where(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).ToList();
        if (cpuLimitSamples.Any() && !double.IsNaN(cpuLimitSamples.First().value))
        {
            Assert.True(cpuLimitSamples.First().value <= 1.0);
        }
    }

    [Fact]
    public void Provider_WithCalculationV2_ReportsContainerCpuTime_AsCounterInSeconds()
    {
        var options = Options.Options.Create(new ResourceMonitoringOptions { UseLinuxCalculationV2 = true });
        using var meter = new Meter(nameof(Provider_WithCalculationV2_ReportsContainerCpuTime_AsCounterInSeconds));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var cpuStatFile = new FileInfo("/sys/fs/cgroup/fakeslice/cpu.stat");
        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/fakeslice" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { cpuStatFile, "usage_usec 2000000\nuser_usec 1500000\nsystem_usec 500000\nnr_periods 10\nnr_throttled 0\nthrottled_usec 0"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.max"), "200000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.weight"), "100"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _ = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, new FakeLogger<LinuxUtilizationProvider>(), fakeTime);

        Instrument? cpuTimeInstrument = null;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter) && instrument.Name == ResourceUtilizationInstruments.ContainerCpuTime)
                {
                    cpuTimeInstrument = instrument;
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var samples = new List<(double value, KeyValuePair<string, object?>[] tags)>();
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            if (ReferenceEquals(cpuTimeInstrument, instrument))
            {
                samples.Add((value, tags.ToArray()));
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        var counter = Assert.IsType<ObservableCounter<double>>(cpuTimeInstrument);
        Assert.Equal("s", counter.Unit);
        Assert.Equal("CPU time used by the container.", counter.Description);
        Assert.Collection(
            samples,
            sample => AssertCpuTimeSample(sample, "user", 1.5),
            sample => AssertCpuTimeSample(sample, "system", 0.5));

        // The values are the cumulative CPU time from cpu.stat, not a rate.
        fileSystem.ReplaceFileContent(cpuStatFile, "usage_usec 3000000\nuser_usec 2250000\nsystem_usec 750000\nnr_periods 20\nnr_throttled 0\nthrottled_usec 0");
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        samples.Clear();
        listener.RecordObservableInstruments();

        Assert.Collection(
            samples,
            sample => AssertCpuTimeSample(sample, "user", 2.25),
            sample => AssertCpuTimeSample(sample, "system", 0.75));
    }

    [Fact]
    public void Provider_WithCalculationV2_ObservingContainerCpuTime_DoesNotAffectCpuUtilization()
    {
        var options = Options.Options.Create(new ResourceMonitoringOptions { UseLinuxCalculationV2 = true });
        using var meter = new Meter(nameof(Provider_WithCalculationV2_ObservingContainerCpuTime_DoesNotAffectCpuUtilization));
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var cpuStatFile = new FileInfo("/sys/fs/cgroup/fakeslice/cpu.stat");
        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/self/cgroup"), "0::/fakeslice" },
            { new FileInfo("/sys/fs/cgroup/memory.max"), "2097152" },
            { new FileInfo("/proc/stat"), "cpu 100 10 10 10 10 10 10 10 10 10"},
            { cpuStatFile, "usage_usec 1000000\nuser_usec 800000\nsystem_usec 200000\nnr_periods 10"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 2048 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-3"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.max"), "200000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 0"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "1048576"},
            { new FileInfo("/sys/fs/cgroup/fakeslice/cpu.weight"), "100"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var resourceQuotaProvider = new LinuxResourceQuotaProvider(parser, options);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, resourceQuotaProvider, new FakeLogger<LinuxUtilizationProvider>(), fakeTime);

        using var cpuTimeListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter) && instrument.Name == ResourceUtilizationInstruments.ContainerCpuTime)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var cpuTimeSamples = new List<double>();
        cpuTimeListener.SetMeasurementEventCallback<double>((_, value, _, _) => cpuTimeSamples.Add(value));
        cpuTimeListener.Start();

        using var cpuUtilizationListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(meter, instrument.Meter) &&
                    (instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization ||
                     instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        var cpuUtilizationSamples = new Dictionary<string, double>();
        cpuUtilizationListener.SetMeasurementEventCallback<double>((instrument, value, _, _) => cpuUtilizationSamples[instrument.Name] = value);
        cpuUtilizationListener.Start();

        // Only container.cpu.time is observed while the container uses 1 core (100 ms in 1 period of 100 ms)...
        fileSystem.ReplaceFileContent(cpuStatFile, "usage_usec 1100000\nuser_usec 880000\nsystem_usec 220000\nnr_periods 11");
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        cpuTimeListener.RecordObservableInstruments();
        Assert.Equal(new[] { 0.88, 0.22 }, cpuTimeSamples);

        // ...and then the container uses 0.25 cores (100 ms in 4 periods of 100 ms).
        fileSystem.ReplaceFileContent(cpuStatFile, "usage_usec 1200000\nuser_usec 960000\nsystem_usec 240000\nnr_periods 15");
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        cpuUtilizationListener.RecordObservableInstruments();

        // The CPU utilization covers the whole time since the provider was created (200 ms in 5 periods of 100 ms = 0.4 cores),
        // because observing container.cpu.time doesn't consume the CPU usage used by the utilization instruments.
        const double CoresUsed = 0.4;
        Assert.Equal(2, cpuUtilizationSamples.Count);
        Assert.Equal(CoresUsed / provider.Resources.MaximumCpuUnits, cpuUtilizationSamples[ResourceUtilizationInstruments.ContainerCpuLimitUtilization]);
        Assert.Equal(CoresUsed / provider.Resources.GuaranteedCpuUnits, cpuUtilizationSamples[ResourceUtilizationInstruments.ContainerCpuRequestUtilization]);
    }

    private static void AssertCpuTimeSample((double value, KeyValuePair<string, object?>[] tags) sample, string expectedCpuMode, double expectedSeconds)
    {
        Assert.Equal(expectedSeconds, sample.value);
        Assert.Equal(new KeyValuePair<string, object?>("cpu.mode", expectedCpuMode), Assert.Single(sample.tags));
    }
}

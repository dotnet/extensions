// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Shared.Instruments;
using Microsoft.TestUtilities;
using Moq;
using VerifyXunit;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific tests")]
[UsesVerify]
public sealed class LinuxUtilizationProviderTests
{
    private const string VerifiedDataDirectory = "Verified";

    [ConditionalFact]
    [CombinatorialData]
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
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, logger, TimeProvider.System);

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

        Assert.Equal(5, samples.Count);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization);
        Assert.Equal(0.5, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessCpuUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization);
        Assert.Equal(0.5, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ProcessMemoryUtilization).value);
    }

    [ConditionalFact]
    [CombinatorialData]
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
            { new FileInfo("/sys/fs/cgroup/memory.max"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), "usage_usec 102312"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.max"), "20000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 312312"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "524288423423"},
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), "4"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, logger, TimeProvider.System);

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

        Assert.Equal(5, samples.Count);

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuLimitUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization);
        Assert.True(double.IsNaN(samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerCpuRequestUtilization).value));

        Assert.Contains(samples, x => x.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization);
        Assert.Equal(1, samples.Single(i => i.instrument.Name == ResourceUtilizationInstruments.ContainerMemoryLimitUtilization).value);

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
            { new FileInfo("/sys/fs/cgroup/memory.max"), "9223372036854771712" },
            { new FileInfo("/proc/stat"), "cpu 10 10 10 10 10 10 10 10 10 10"},
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), "usage_usec 102312"},
            { new FileInfo("/proc/meminfo"), "MemTotal: 1024 kB"},
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0-19"},
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.max"), "20000 100000"},
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 312312"},
            { new FileInfo("/sys/fs/cgroup/memory.current"), "524288423423"},
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), "4"},
        });

        var parser = new LinuxUtilizationParserCgroupV2(fileSystem: fileSystem, new FakeUserHz(100));

        var snapshotProvider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, logger, TimeProvider.System);
        var logRecords = logger.Collector.GetSnapshot();

        return Verifier.Verify(logRecords).UseDirectory(VerifiedDataDirectory);
    }

    [Fact]
    public void Provider_Creates_Meter_With_Correct_Name()
    {
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meterFactory = new TestMeterFactory();

        var parser = new DummyLinuxUtilizationParser();
        _ = new LinuxUtilizationProvider(options, parser, meterFactory);

        var meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }
}

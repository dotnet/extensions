// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package")]
public sealed class LinuxCountersTests
{
    [ConditionalFact]
    public void LinuxCounters_Registers_Instruments()
    {
        var meterName = Guid.NewGuid().ToString();
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter(nameof(LinuxCounters_Registers_Instruments));
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
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, TimeProvider.System);

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

        Assert.Equal(2, samples.Count);
        Assert.Equal(ResourceUtilizationInstruments.CpuUtilization, samples[0].instrument.Name);
        Assert.Equal(double.NaN, samples[0].value);
        Assert.Equal(ResourceUtilizationInstruments.MemoryUtilization, samples[1].instrument.Name);
        Assert.Equal(0.5, samples[1].value);
    }

    [ConditionalFact]
    public void LinuxCounters_Registers_Instruments_CgroupV2()
    {
        var meterName = Guid.NewGuid().ToString();
        var options = Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter(nameof(LinuxCounters_Registers_Instruments));
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
        var provider = new LinuxUtilizationProvider(options, parser, meterFactoryMock.Object, TimeProvider.System);

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

        Assert.Equal(2, samples.Count);
        Assert.Equal(ResourceUtilizationInstruments.CpuUtilization, samples[0].instrument.Name);
        Assert.Equal(double.NaN, samples[0].value);
        Assert.Equal(ResourceUtilizationInstruments.MemoryUtilization, samples[1].instrument.Name);
        Assert.Equal(1, samples[1].value);
    }
}

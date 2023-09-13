// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Telemetry.Metrics;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package")]
public sealed class LinuxCountersTest
{
    [ConditionalFact]
    public void LinuxCounters_Registers_Instruments()
    {
        var meterName = Guid.NewGuid().ToString();
        var options = Microsoft.Extensions.Options.Options.Create<ResourceMonitoringOptions>(new());
        using var meter = new Meter<LinuxUtilizationProvider>();
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
        });
        var parser = new LinuxUtilizationParser(fileSystem: fileSystem, new FakeUserHz(100));
        var provider = new LinuxUtilizationProvider(options, parser, meter, TimeProvider.System);

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                listener.EnableMeasurementEvents(instrument);
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
        Assert.Equal(ResourceUtilizationCounters.CpuConsumptionPercentage, samples[0].instrument.Name);
        Assert.Equal(double.NaN, samples[0].value);
        Assert.Equal(ResourceUtilizationCounters.MemoryConsumptionPercentage, samples[1].instrument.Name);
        Assert.Equal(50, samples[1].value);
    }
}

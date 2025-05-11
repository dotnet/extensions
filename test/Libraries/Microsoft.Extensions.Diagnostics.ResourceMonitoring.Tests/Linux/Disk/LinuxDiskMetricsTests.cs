// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Shared.Instruments;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific tests")]
public class LinuxDiskMetricsTests
{
    private readonly FakeLogger<LinuxDiskMetrics> _fakeLogger = new();

    [Fact]
    public void Creates_Meter_With_Correct_Name()
    {
        using var meterFactory = new TestMeterFactory();
        var diskStatsReaderMock = new Mock<IDiskStatsReader>();
        var options = new ResourceMonitoringOptions { EnableDiskIoMetrics = true };

        _ = new LinuxDiskMetrics(
            _fakeLogger,
            meterFactory,
            Options.Options.Create(options),
            TimeProvider.System,
            diskStatsReaderMock.Object);

        Meter meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Shared.Instruments;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific tests")]
public class LinuxNetworkMetricsTests
{
    [Fact]
    public void Creates_Meter_With_Correct_Name()
    {
        using var meterFactory = new TestMeterFactory();
        var tcpStateInfoProviderMock = new Mock<ITcpStateInfoProvider>();
        _ = new LinuxNetworkMetrics(meterFactory, tcpStateInfoProviderMock.Object);

        Meter meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }
}

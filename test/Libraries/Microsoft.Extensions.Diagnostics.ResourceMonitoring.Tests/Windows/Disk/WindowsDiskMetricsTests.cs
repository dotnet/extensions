// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Shared.Instruments;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk.Test;

[SupportedOSPlatform("windows")]
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public class WindowsDiskMetricsTests
{
    [ConditionalFact]
    public void Creates_Meter_With_Correct_Name()
    {
        using var meterFactory = new TestMeterFactory();
        var performanceCounterFactoryMock = new Mock<IPerformanceCounterFactory>();
        var options = new ResourceMonitoringOptions { EnableDiskIoMetrics = true };

        _ = new WindowsDiskMetrics(meterFactory, performanceCounterFactoryMock.Object, Microsoft.Extensions.Options.Options.Create(options));

        Meter meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }
}

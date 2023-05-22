// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class FaultInjectionTelemetryHandlerTests
{
    private const string MetricName = @"R9\Resilience\FaultInjection\InjectedFaults";

    [Fact]
    public void LogAndMeter()
    {
        var logger = Mock.Of<ILogger<IChaosPolicyFactory>>();

        using var meter = new Meter<IChaosPolicyFactory>();
        using var metricCollector = new MetricCollector(meter);
        var counter = meter.CreateCounter<long>(MetricName);
        var metricCounter = new FaultInjectionMetricCounter(counter);

        const string GroupName = "TestClient";
        const string FaultType = "Type";
        const string InjectedValue = "Value";

        FaultInjectionTelemetryHandler.LogAndMeter(logger, metricCounter, GroupName, FaultType, InjectedValue);

        var latest = metricCollector.GetCounterValues<long>(MetricName)!.LatestWritten;

        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Equal(GroupName, latest.GetDimension(FaultInjectionEventMeterDimensions.FaultInjectionGroupName));
        Assert.Equal(FaultType, latest.GetDimension(FaultInjectionEventMeterDimensions.FaultType));
        Assert.Equal(InjectedValue, latest.GetDimension(FaultInjectionEventMeterDimensions.InjectedValue));
    }
}

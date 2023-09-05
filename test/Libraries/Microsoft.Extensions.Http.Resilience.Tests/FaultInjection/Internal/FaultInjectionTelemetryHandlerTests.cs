// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Extensions.Telemetry.Metrics;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Telemetry.Testing.Metrics;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal.Test;

public class FaultInjectionTelemetryHandlerTests
{
    private const string MetricName = @"R9\Resilience\FaultInjection\HttpClient\InjectedFaults";

    [Fact]
    public void LogAndMeter_WithHttpContentKey()
    {
        var logger = new FakeLogger();

        using var meter = new Meter<IChaosPolicyFactory>();
        using var metricCollector = new MetricCollector<long>(meter, MetricName);
        var counter = meter.CreateCounter<long>(MetricName);
        var metricCounter = new HttpClientFaultInjectionMetricCounter(counter);

        const string GroupName = "TestClient";
        const string FaultType = "Type";
        const string InjectedValue = "Value";
        const string HttpContentKey = "HttpContentKey";

        FaultInjectionTelemetryHandler.LogAndMeter(logger, metricCounter, GroupName, FaultType, InjectedValue, HttpContentKey);

        var latest = metricCollector.LastMeasurement!;

        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Equal(GroupName, latest.Tags[FaultInjectionEventMeterTagNames.FaultInjectionGroupName]);
        Assert.Equal(FaultType, latest.Tags[FaultInjectionEventMeterTagNames.FaultType]);
        Assert.Equal(InjectedValue, latest.Tags[FaultInjectionEventMeterTagNames.InjectedValue]);
        Assert.Equal(HttpContentKey, latest.Tags[FaultInjectionEventMeterTagNames.HttpContentKey]);

        var entries = logger.Collector.GetSnapshot();
        entries.Should().HaveCount(1);
        entries[0].Message.Should().Be(
            "Fault-injection group name: TestClient. " +
            "Fault type: Type. " +
            "Injected value: Value. " +
            "Http content key: HttpContentKey.");
    }

    [Fact]
    public void LogAndMeter_WithoutHttpContentKey()
    {
        var logger = Mock.Of<ILogger<IHttpClientChaosPolicyFactory>>();

        using var meter = new Meter<IChaosPolicyFactory>();
        using var metricCollector = new MetricCollector<long>(meter, MetricName);
        var counter = meter.CreateCounter<long>(MetricName);
        var metricCounter = new HttpClientFaultInjectionMetricCounter(counter);

        const string GroupName = "TestClient";
        const string FaultType = "Type";
        const string InjectedValue = "Value";
        const string HttpContentKey = "N/A";

        FaultInjectionTelemetryHandler.LogAndMeter(logger, metricCounter, GroupName, FaultType, InjectedValue, httpContentKey: null);

        var latest = metricCollector.LastMeasurement;

        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Equal(GroupName, latest.Tags[FaultInjectionEventMeterTagNames.FaultInjectionGroupName]);
        Assert.Equal(FaultType, latest.Tags[FaultInjectionEventMeterTagNames.FaultType]);
        Assert.Equal(InjectedValue, latest.Tags[FaultInjectionEventMeterTagNames.InjectedValue]);
        Assert.Equal(HttpContentKey, latest.Tags[FaultInjectionEventMeterTagNames.HttpContentKey]);
    }
}

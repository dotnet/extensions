// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Metrics;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Telemetry.Testing.Metrics;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Common.Tests;

public class TelemetryHealthChecksPublisherTest
{
    private const string HealthReportMetricName = @"R9\HealthCheck\Report";
    private const string UnhealthyHealthCheckMetricName = @"R9\HealthCheck\UnhealthyHealthCheck";

    public static TheoryData<List<HealthStatus>, bool, int, string, LogLevel, string, string> PublishAsyncArgs => new()
    {
        {
            new List<HealthStatus> { HealthStatus.Healthy },
            false,
            1,
            "Process reporting healthy: Healthy.",
            LogLevel.Debug,
            bool.TrueString,
            HealthStatus.Healthy.ToString()
        },
        {
            new List<HealthStatus> { HealthStatus.Degraded },
            true,
            1,
            "Process reporting unhealthy: Degraded. Health check entries are id0: {status: Degraded, description: desc0}",
            LogLevel.Warning,
            bool.FalseString,
            HealthStatus.Degraded.ToString()
        },
        {
            new List<HealthStatus> { HealthStatus.Unhealthy },
            false,
            1,
            "Process reporting unhealthy: Unhealthy. Health check entries are id0: {status: Unhealthy, description: desc0}",
            LogLevel.Warning,
            bool.FalseString,
            HealthStatus.Unhealthy.ToString()
        },
        {
            new List<HealthStatus> { HealthStatus.Healthy, HealthStatus.Healthy },
            true,
            0,
            "Process reporting healthy: Healthy.",
            LogLevel.Debug,
            bool.TrueString,
            HealthStatus.Healthy.ToString()
        },
        {
            new List<HealthStatus> { HealthStatus.Healthy, HealthStatus.Unhealthy },
            true,
            1,
            "Process reporting unhealthy: Unhealthy. Health check entries are id0: {status: Healthy, description: desc0}, id1: {status: Unhealthy, description: desc1}",
            LogLevel.Warning,
            bool.FalseString,
            HealthStatus.Unhealthy.ToString()
        },
        {
            new List<HealthStatus> { HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy },
            false,
            1,
            "Process reporting unhealthy: Unhealthy. Health check entries are " +
            "id0: {status: Healthy, description: desc0}, id1: {status: Degraded, description: desc1}, id2: {status: Unhealthy, description: desc2}",
            LogLevel.Warning,
            bool.FalseString,
            HealthStatus.Unhealthy.ToString()
        },
    };

    [Theory]
    [MemberData(nameof(PublishAsyncArgs))]
    public async Task PublishAsync(
        IList<HealthStatus> healthStatuses,
        bool logOnlyUnhealthy,
        int expectedLogCount,
        string expectedLogMessage,
        LogLevel expectedLogLevel,
        string expectedMetricHealthy,
        string expectedMetricStatus)
    {
        using var meter = new Meter<TelemetryHealthCheckPublisher>();
        using var healthyMetricCollector = new MetricCollector<long>(meter, HealthReportMetricName);
        using var unhealthyMetricCollector = new MetricCollector<long>(meter, UnhealthyHealthCheckMetricName);

        var logger = new FakeLogger<TelemetryHealthCheckPublisher>();
        var collector = logger.Collector;

        var options = Microsoft.Extensions.Options.Options.Create(new TelemetryHealthCheckPublisherOptions
        {
            LogOnlyUnhealthy = logOnlyUnhealthy,
        });

        var publisher = new TelemetryHealthCheckPublisher(meter, logger, options);

        await publisher.PublishAsync(CreateHealthReport(healthStatuses), CancellationToken.None);
        Assert.Equal(expectedLogCount, collector.Count);
        if (expectedLogCount > 0)
        {
            Assert.Equal(expectedLogMessage, collector.LatestRecord.Message);
            Assert.Equal(expectedLogLevel, collector.LatestRecord.Level);
        }

        var latest = healthyMetricCollector.LastMeasurement!;

        latest.Value.Should().Be(1);
        latest.Tags["healthy"].Should().Be(expectedMetricHealthy);
        latest.Tags["status"].Should().Be(expectedMetricStatus);

        var unhealthyCounters = unhealthyMetricCollector.GetMeasurementSnapshot();

        for (int i = 0; i < healthStatuses.Count; i++)
        {
            var healthStatus = healthStatuses[i];
            if (healthStatus != HealthStatus.Healthy)
            {
                Assert.Equal(1, GetValue(unhealthyCounters, GetKey(i), healthStatuses[i].ToString()));
            }
        }
    }

    [Fact]
    public void Ctor_ThrowsWhenOptionsValueNull()
    {
        using var meter = new Meter<TelemetryHealthCheckPublisher>();
        var logger = new FakeLogger<TelemetryHealthCheckPublisher>();
        Assert.Throws<ArgumentException>(() => new TelemetryHealthCheckPublisher(meter, logger, Microsoft.Extensions.Options.Options.Create<TelemetryHealthCheckPublisherOptions>(null!)));
    }

    private static long GetValue(IReadOnlyCollection<CollectedMeasurement<long>> counters, string healthy, string status)
    {
        foreach (var counter in counters)
        {
            if (counter!.Tags["name"]?.ToString() == healthy &&
                counter!.Tags["status"]?.ToString() == status)
            {
                return counter.Value;
            }
        }

        return 0;
    }

    private static HealthReport CreateHealthReport(IEnumerable<HealthStatus> healthStatuses)
    {
        var healthStatusRecords = new Dictionary<string, HealthReportEntry>();

        int index = 0;
        foreach (var status in healthStatuses)
        {
            var entry = new HealthReportEntry(status, $"desc{index}", TimeSpan.Zero, null, null);
            healthStatusRecords.Add(GetKey(index), entry);
            index++;
        }

        return new HealthReport(healthStatusRecords, TimeSpan.Zero);
    }

    private static string GetKey(int index) => $"id{index}";
}

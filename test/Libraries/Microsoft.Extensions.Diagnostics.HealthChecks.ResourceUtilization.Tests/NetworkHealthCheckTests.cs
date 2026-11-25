// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class NetworkHealthCheckTests
{
    private const string ResourceMonitoringMeterName = "Microsoft.Extensions.Diagnostics.ResourceMonitoring";
    private const string NetworkConnectionsInstrumentName = "system.network.connections";
    [Theory]
    [ClassData(typeof(NetworkHealthCheckTestData))]
    public async Task TestNetworkChecks(HealthStatus expected, long establishedConnections, long synReceivedConnections,
        ResourceCountThresholds establishedThresholds, ResourceCountThresholds synReceivedThresholds, string expectedDescription)
    {
        var checkOptions = new NetworkUtilizationHealthCheckOptions
        {
            EstablishedConnectionsThresholds = establishedThresholds,
            SynReceivedConnectionsThresholds = synReceivedThresholds,
        };

        HealthCheckResult healthCheckResult = await NetworkUtilizationHealthCheck.EvaluateHealthStatusAsync(
            establishedConnections, synReceivedConnections, checkOptions);

        Assert.Equal(expected, healthCheckResult.Status);
        Assert.NotEmpty(healthCheckResult.Data);
        Assert.Equal(establishedConnections, healthCheckResult.Data["EstablishedConnections"]);
        Assert.Equal(synReceivedConnections, healthCheckResult.Data["SynReceivedConnections"]);
        if (healthCheckResult.Status != HealthStatus.Healthy)
        {
            Assert.Equal(expectedDescription, healthCheckResult.Description);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ReadsNetworkConnectionsFromInstruments()
    {
        using Meter meter = new(ResourceMonitoringMeterName);
        _ = meter.CreateObservableUpDownCounter(
            NetworkConnectionsInstrumentName,
            GetMeasurements,
            unit: "{connection}");

        var checkOptions = new NetworkUtilizationHealthCheckOptions
        {
            EstablishedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 10_000, UnhealthyCount = 20_000 },
            SynReceivedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 10_000, UnhealthyCount = 20_000 },
        };

        using var healthCheck = new NetworkUtilizationHealthCheck(Microsoft.Extensions.Options.Options.Create(checkOptions));
        HealthCheckResult healthCheckResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, healthCheckResult.Status);
        Assert.Equal(15L, healthCheckResult.Data["EstablishedConnections"]);
        Assert.Equal(3L, healthCheckResult.Data["SynReceivedConnections"]);

        static IEnumerable<Measurement<long>> GetMeasurements()
        {
            // IPv4 established (10) + IPv6 established (5) are summed up.
            yield return new Measurement<long>(10, new TagList { new("network.type", "ipv4"), new("system.network.state", "established") });
            yield return new Measurement<long>(5, new TagList { new("network.type", "ipv6"), new("system.network.state", "established") });

            // IPv4 SYN received (2) + IPv6 SYN received (1) are summed up.
            yield return new Measurement<long>(2, new TagList { new("network.type", "ipv4"), new("system.network.state", "syn_recv") });
            yield return new Measurement<long>(1, new TagList { new("network.type", "ipv6"), new("system.network.state", "syn_recv") });

            // Other connection states are ignored by the health check.
            yield return new Measurement<long>(7, new TagList { new("network.type", "ipv4"), new("system.network.state", "listen") });
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ReportsUnhealthyWhenConnectionsExceedThreshold()
    {
        using Meter meter = new(ResourceMonitoringMeterName);
        _ = meter.CreateObservableUpDownCounter(
            NetworkConnectionsInstrumentName,
            () => new[]
            {
                new Measurement<long>(1500, new TagList { new("network.type", "ipv4"), new("system.network.state", "established") }),
            },
            unit: "{connection}");

        var checkOptions = new NetworkUtilizationHealthCheckOptions
        {
            EstablishedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 500, UnhealthyCount = 1000 },
        };

        using var healthCheck = new NetworkUtilizationHealthCheck(Microsoft.Extensions.Options.Options.Create(checkOptions));
        HealthCheckResult healthCheckResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, healthCheckResult.Status);
        Assert.Equal("Established connections count is above the limit", healthCheckResult.Description);
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentException>(() => new NetworkUtilizationHealthCheck(
            Mock.Of<IOptions<NetworkUtilizationHealthCheckOptions>>()));
    }
}

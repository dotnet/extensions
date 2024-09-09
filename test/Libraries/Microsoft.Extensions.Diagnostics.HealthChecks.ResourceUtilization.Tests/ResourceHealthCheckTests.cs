// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class ResourceHealthCheckTests
{
    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[]
            {
                HealthStatus.Healthy,
                0.1,
                0UL,
                1000UL,
                new ResourceUsageThresholds(),
                new ResourceUsageThresholds(),
                "",
            },
            new object[]
            {
                HealthStatus.Healthy,
                0.2,
                0UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                ""
            },
            new object[]
            {
                HealthStatus.Healthy,
                0.2,
                2UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                ""
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.4,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.4, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.4, UnhealthyUtilizationPercentage = 0.2 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.3,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2 },
                "CPU and memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.3,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                "CPU usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.1,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "Memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                "CPU usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.1,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "Memory usage is above the limit"
            },
        };

    [Theory]
    [MemberData(nameof(Data))]
    public async Task TestCpuAndMemoryChecks(HealthStatus expected, double utilization, ulong memoryUsed, ulong totalMemory,
        ResourceUsageThresholds cpuThresholds, ResourceUsageThresholds memoryThresholds, string expectedDescription)
    {
        var systemResources = new SystemResources(1.0, 1.0, totalMemory, totalMemory);
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);
        dataTracker
            .Setup(tracker => tracker.GetUtilization(samplingWindow))
            .Returns(new ResourceUtilization(cpuUsedPercentage: utilization, memoryUsedInBytes: memoryUsed, systemResources));

        var checkContext = new HealthCheckContext();
        var checkOptions = new ResourceUtilizationHealthCheckOptions
        {
            CpuThresholds = cpuThresholds,
            MemoryThresholds = memoryThresholds,
            SamplingWindow = samplingWindow
        };

        var options = Microsoft.Extensions.Options.Options.Create(checkOptions);
        var healthCheck = new ResourceUtilizationHealthCheck(options, dataTracker.Object);
        var healthCheckResult = await healthCheck.CheckHealthAsync(checkContext);
        Assert.Equal(expected, healthCheckResult.Status);
        Assert.NotEmpty(healthCheckResult.Data);
        if (healthCheckResult.Status != HealthStatus.Healthy)
        {
            Assert.Equal(expectedDescription, healthCheckResult.Description);
        }
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentException>(() => new ResourceUtilizationHealthCheck(Mock.Of<IOptions<ResourceUtilizationHealthCheckOptions>>(), null!));
    }
}

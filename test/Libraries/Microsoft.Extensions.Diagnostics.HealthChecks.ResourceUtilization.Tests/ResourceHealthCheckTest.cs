// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Tests;

public class ResourceHealthCheckTest
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
                "",
            },
            new object[]
            {
                HealthStatus.Healthy,
                0.2,
                0UL,
                1000UL,
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
                ""
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.4,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                " usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                " usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.4, UnhealthyUtilizationPercentage = 0.2 },
                " usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.3,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2 },
                " usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { UnhealthyUtilizationPercentage = 0.4 },
                " usage is above the limit"
            },
        };

    [Theory]
    [MemberData(nameof(Data))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public async Task TestCpuChecks(HealthStatus expected, double utilization, ulong _, ulong totalMemory, ResourceUsageThresholds thresholds, string expectedDescription)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        var systemResources = new SystemResources(1.0, 1.0, totalMemory, totalMemory);
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);
        dataTracker
            .Setup(tracker => tracker.GetUtilization(samplingWindow))
            .Returns(new ResourceUtilization(cpuUsedPercentage: utilization, memoryUsedInBytes: 0, systemResources));

        var checkContext = new HealthCheckContext();
        var cpuCheckOptions = new ResourceUtilizationHealthCheckOptions
        {
            CpuThresholds = thresholds,
            SamplingWindow = samplingWindow
        };

        var options = Microsoft.Extensions.Options.Options.Create(cpuCheckOptions);
        var healthCheck = new ResourceUtilizationHealthCheck(options, dataTracker.Object);
        var healthCheckResult = await healthCheck.CheckHealthAsync(checkContext);
        Assert.Equal(expected, healthCheckResult.Status);
        if (healthCheckResult.Status != HealthStatus.Healthy)
        {
            Assert.Equal("CPU" + expectedDescription, healthCheckResult.Description);
        }
    }

    [Theory]
    [MemberData(nameof(Data))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public async Task TestMemoryChecks(HealthStatus expected, double _, ulong memoryUsed, ulong totalMemory, ResourceUsageThresholds thresholds, string expectedDescription)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        var systemResources = new SystemResources(1.0, 1.0, totalMemory, totalMemory);
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);
        dataTracker
            .Setup(tracker => tracker.GetUtilization(samplingWindow))
            .Returns(new ResourceUtilization(cpuUsedPercentage: 0, memoryUsedInBytes: memoryUsed, systemResources));

        var checkContext = new HealthCheckContext();
        var memCheckOptions = new ResourceUtilizationHealthCheckOptions
        {
            MemoryThresholds = thresholds,
            SamplingWindow = samplingWindow
        };

        var options = Microsoft.Extensions.Options.Options.Create(memCheckOptions);
        var healthCheck = new ResourceUtilizationHealthCheck(options, dataTracker.Object);
        var healthCheckResult = await healthCheck.CheckHealthAsync(checkContext);
        Assert.Equal(expected, healthCheckResult.Status);
        if (healthCheckResult.Status != HealthStatus.Healthy)
        {
            Assert.Equal("Memory" + expectedDescription, healthCheckResult.Description);
        }
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentException>(() => new ResourceUtilizationHealthCheck(Mock.Of<IOptions<ResourceUtilizationHealthCheckOptions>>(), null!));
    }
}

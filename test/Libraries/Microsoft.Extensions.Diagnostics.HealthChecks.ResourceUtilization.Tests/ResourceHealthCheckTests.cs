// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class ResourceHealthCheckTests
{
    [Theory]
    [ClassData(typeof(HealthCheckTestData))]
    public async Task TestCpuAndMemoryChecks(HealthStatus expected, double utilization, ulong memoryUsed, ulong totalMemory,
        ResourceUsageThresholds cpuThresholds, ResourceUsageThresholds memoryThresholds,
        string expectedDescription)
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
        using var autoResetEvent = new AutoResetEvent(false);
        using var healthCheck = new ResourceUtilizationHealthCheck(options, dataTracker.Object);
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

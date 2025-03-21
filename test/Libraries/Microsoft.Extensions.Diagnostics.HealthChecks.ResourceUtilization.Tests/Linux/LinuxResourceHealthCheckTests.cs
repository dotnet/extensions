// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class LinuxResourceHealthCheckTests
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

    [ConditionalTheory]
    [MemberData(nameof(Data))]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux-specific test.")]
    public async Task TestCpuAndMemoryChecks_WithMetrics(
        HealthStatus expected, double utilization, ulong memoryUsed, ulong totalMemory,
        ResourceUsageThresholds cpuThresholds, ResourceUsageThresholds memoryThresholds,
        string expectedDescription)
    {
        var fakeClock = new FakeTimeProvider();
        var dataTracker = new Mock<IResourceMonitor>();
        var meterName = Guid.NewGuid().ToString();
        var logger = new FakeLogger<LinuxUtilizationProvider>();
        using var meter = new Meter("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
        var meterFactoryMock = new Mock<IMeterFactory>();
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meter);

        var parser = new Mock<ILinuxUtilizationParser>();
        parser.Setup(x => x.GetHostCpuCount()).Returns(1);
        parser.Setup(x => x.GetCgroupLimitedCpus()).Returns(1);
        parser.Setup(x => x.GetCgroupRequestCpu()).Returns(1);
        parser.SetupSequence(x => x.GetHostCpuUsageInNanoseconds())
            .Returns(0)
            .Returns(1000);
        parser.SetupSequence(x => x.GetCgroupCpuUsageInNanoseconds())
            .Returns(0)
            .Returns((long)(10 * utilization));
        parser.Setup(x => x.GetMemoryUsageInBytes()).Returns(memoryUsed);
        parser.Setup(x => x.GetAvailableMemoryInBytes()).Returns(totalMemory);

        var provider = new LinuxUtilizationProvider(Options.Options.Create<ResourceMonitoringOptions>(new()), parser.Object, meterFactoryMock.Object, logger, fakeClock);

        var checkContext = new HealthCheckContext();
        var checkOptions = new ResourceUtilizationHealthCheckOptions
        {
            CpuThresholds = cpuThresholds,
            MemoryThresholds = memoryThresholds,
            UseObservableResourceMonitoringInstruments = true
        };

        var options = Microsoft.Extensions.Options.Options.Create(checkOptions);
        using var healthCheck = new ResourceUtilizationHealthCheck(
            options,
            dataTracker.Object,
            Microsoft.Extensions.Options.Options.Create(new ResourceMonitoringOptions()));

        // Act
        fakeClock.Advance(TimeSpan.FromMilliseconds(1));
        var healthCheckResult = await healthCheck.CheckHealthAsync(checkContext);

        // Assert
        Assert.Equal(expected, healthCheckResult.Status);
        Assert.NotEmpty(healthCheckResult.Data);
        if (healthCheckResult.Status != HealthStatus.Healthy)
        {
            Assert.Equal(expectedDescription, healthCheckResult.Description);
        }
    }
}

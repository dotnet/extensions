// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Tests;

public class ResourceHealthCheckExtensionsTest
{
    [Fact]
    public async Task Extensions_AddResourceUtilizationHealthCheck()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [Fact]
    public async Task Extensions_AddResourceUtilizationHealthCheck_WithAction()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
                o.SamplingWindow = samplingWindow;
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [Fact]
    public async Task Extensions_AddResourceUtilizationHealthCheck_WithActionAndTags()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        var samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
                o.SamplingWindow = samplingWindow;
            },
            new[] { "test" });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [Fact]
    public async Task Extensions_AddResourceUtilizationHealthCheck_WithConfigurationSection()
    {
        var dataTracker = new Mock<IResourceMonitor>();

        var samplingWindow = TimeSpan.FromSeconds(5);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [Fact]
    public async Task Extensions_AddResourceUtilizationHealthCheck_WithConfigurationSectionAndTags()
    {
        var dataTracker = new Mock<IResourceMonitor>();

        var samplingWindow = TimeSpan.FromSeconds(5);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(
                SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)),
                new[] { "test" });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceUtilizationHealthChecksExtensions.AddResourceUtilizationHealthCheck(null!));
        Assert.Throws<ArgumentNullException>(() => ((IHealthChecksBuilder)null!).AddResourceUtilizationHealthCheck((IEnumerable<string>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IHealthChecksBuilder)null!).AddResourceUtilizationHealthCheck((Action<ResourceUtilizationHealthCheckOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IHealthChecksBuilder)null!).AddResourceUtilizationHealthCheck((IConfigurationSection)null!));
    }

    private static IConfiguration SetupResourceHealthCheckConfiguration(
        string cpuDegradedThreshold,
        string cpuUnhealthyThreshold,
        string memoryDegradedThreshold,
        string memoryUnhealthyThreshold,
        string samplingWindow)
    {
        ResourceUtilizationHealthCheckOptions resourceHealthCheckOptions;

#pragma warning disable S103 // Lines should not be too long
        var configurationDict = new Dictionary<string, string?>
            {
               {
                    $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.CpuThresholds)}:{nameof(resourceHealthCheckOptions.CpuThresholds.DegradedUtilizationPercentage)}",
                    cpuDegradedThreshold
               },
               {
                    $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.CpuThresholds)}:{nameof(resourceHealthCheckOptions.CpuThresholds.UnhealthyUtilizationPercentage)}",
                    cpuUnhealthyThreshold
               },
               {
                    $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.MemoryThresholds)}:{nameof(resourceHealthCheckOptions.MemoryThresholds.DegradedUtilizationPercentage)}",
                    memoryDegradedThreshold
               },
               {
                    $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.MemoryThresholds)}:{nameof(resourceHealthCheckOptions.MemoryThresholds.UnhealthyUtilizationPercentage)}",
                    memoryUnhealthyThreshold
               },
               {
                    $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.SamplingWindow)}", samplingWindow
               }
            };
#pragma warning restore S103 // Lines should not be too long

        return new ConfigurationBuilder().AddInMemoryCollection(configurationDict).Build();
    }
}

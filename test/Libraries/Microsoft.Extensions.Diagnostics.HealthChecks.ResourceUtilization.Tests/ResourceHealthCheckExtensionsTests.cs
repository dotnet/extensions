// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

[OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Not supported on MacOs.")]
public class ResourceHealthCheckExtensionsTests
{
    [ConditionalFact]
    public async Task AddResourceHealthCheck()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithCustomResourceMonitorAddedAfterInternalResourceMonitor_OverridesIt()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow).Services
            .AddSingleton(dataTracker.Object);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_RegistersInternalResourceMonitoring()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithTags()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow, "test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithTags_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck("test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithTags_WithCustomResourceMonitorAddedAfterInternalResourceMonitor_OverridesIt()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow, "test").Services
            .AddSingleton(dataTracker.Object);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithTagsEnumerable()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(options =>
                options.SamplingWindow = samplingWindow, new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithTagsEnumerable_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithAction()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

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

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithAction_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
            });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithActionAndTags()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

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
            "test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithActionAndTags_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
            },
            "test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithActionAndTagsEnumerable()
    {
        var dataTracker = new Mock<IResourceMonitor>();
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

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
            new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithActionAndTagsEnumerable_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
            },
            new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithConfigurationSection()
    {
        var dataTracker = new Mock<IResourceMonitor>();

        TimeSpan samplingWindow = TimeSpan.FromSeconds(5);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)));

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithConfigurationSection_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)));

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithConfigurationSectionAndTags()
    {
        var dataTracker = new Mock<IResourceMonitor>();

        TimeSpan samplingWindow = TimeSpan.FromSeconds(5);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(
                SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)),
                "test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithConfigurationSectionAndTags_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)),
                "test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public async Task AddResourceHealthCheck_WithConfigurationSectionAndTagsEnumerable()
    {
        var dataTracker = new Mock<IResourceMonitor>();

        TimeSpan samplingWindow = TimeSpan.FromSeconds(5);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddSingleton(dataTracker.Object)
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(
                SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)),
                new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        _ = await service.CheckHealthAsync();
        dataTracker.Verify(tracker => tracker.GetUtilization(samplingWindow), Times.Once);
    }

    [ConditionalFact]
    public void AddResourceHealthCheck_WithConfigurationSectionAndTagsEnumerable_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(
                SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)),
                new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);
    }

    [ConditionalFact]
    public void ConfigureResourceUtilizationHealthCheck_WithAction()
    {
        TimeSpan samplingWindow = TimeSpan.FromSeconds(1);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(o =>
            {
                o.CpuThresholds = new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 };
                o.SamplingWindow = samplingWindow;
            });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        ResourceUtilizationHealthCheckOptions options = serviceProvider.GetRequiredService<IOptions<ResourceUtilizationHealthCheckOptions>>().Value;

        Assert.Equal(samplingWindow, options.SamplingWindow);
        Assert.Equal(0.2, options.CpuThresholds.DegradedUtilizationPercentage);
        Assert.Equal(0.4, options.CpuThresholds.UnhealthyUtilizationPercentage);
    }

    [ConditionalFact]
    public void ConfigureResourceUtilizationHealthCheck_WithConfigurationSection()
    {
        TimeSpan samplingWindow = TimeSpan.FromSeconds(5);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddHealthChecks()
            .AddResourceUtilizationHealthCheck(SetupResourceHealthCheckConfiguration("0.5", "0.7", "0.5", "0.7", "00:00:05").GetSection(nameof(ResourceUtilizationHealthCheckOptions)));

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        ResourceUtilizationHealthCheckOptions options = serviceProvider.GetRequiredService<IOptions<ResourceUtilizationHealthCheckOptions>>().Value;

        Assert.Equal(samplingWindow, options.SamplingWindow);
        Assert.Equal(0.5, options.CpuThresholds.DegradedUtilizationPercentage);
        Assert.Equal(0.7, options.CpuThresholds.UnhealthyUtilizationPercentage);
        Assert.Equal(0.5, options.MemoryThresholds.DegradedUtilizationPercentage);
        Assert.Equal(0.7, options.MemoryThresholds.UnhealthyUtilizationPercentage);
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceUtilizationHealthCheckExtensions.AddResourceUtilizationHealthCheck(null!));
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

        var configurationDict = new Dictionary<string, string?>
        {
            {
                $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.CpuThresholds)}:"
                + $"{nameof(resourceHealthCheckOptions.CpuThresholds.DegradedUtilizationPercentage)}",
                cpuDegradedThreshold
            },
            {
                $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.CpuThresholds)}:"
                + $"{nameof(resourceHealthCheckOptions.CpuThresholds.UnhealthyUtilizationPercentage)}",
                cpuUnhealthyThreshold
            },
            {
                $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.MemoryThresholds)}:"
                + $"{nameof(resourceHealthCheckOptions.MemoryThresholds.DegradedUtilizationPercentage)}",
                memoryDegradedThreshold
            },
            {
                $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.MemoryThresholds)}:"
                +$"{nameof(resourceHealthCheckOptions.MemoryThresholds.UnhealthyUtilizationPercentage)}",
                memoryUnhealthyThreshold
            },
            {
                $"{nameof(ResourceUtilizationHealthCheckOptions)}:{nameof(resourceHealthCheckOptions.SamplingWindow)}", samplingWindow
            }
        };

        return new ConfigurationBuilder().AddInMemoryCollection(configurationDict).Build();
    }
}

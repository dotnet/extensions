// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

[OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Not supported on MacOs.")]
public sealed class ResourceMonitoringExtensionsTests
{
    [ConditionalFact]
    public void Throw_Null_When_Registration_Ingredients_Null()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => ((IResourceMonitorBuilder)null!).ConfigureMonitor(_ => { }));
        Assert.Throws<ArgumentNullException>(() => ((IResourceMonitorBuilder)null!).ConfigureMonitor((IConfigurationSection)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddResourceMonitoring((b) => b.ConfigureMonitor((IConfigurationSection)null!)));
        Assert.Throws<ArgumentNullException>(() => services.AddResourceMonitoring((b) => b.ConfigureMonitor((Action<ResourceMonitoringOptions>)null!)));
    }

    [ConditionalFact]
    public void AddsResourceMonitoringService_ToServicesCollection()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddResourceMonitoring(builder =>
            {
                builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                builder.AddPublisher<EmptyPublisher>();
            })
            .BuildServiceProvider();

        var trackerService = provider.GetRequiredService<IResourceMonitor>();

        Assert.NotNull(trackerService);
        Assert.IsType<ResourceMonitorService>(trackerService);
        Assert.IsAssignableFrom<IResourceMonitor>(trackerService);
    }

    [ConditionalFact]
    public void AddsResourceMonitoringService_ToServicesCollection_NoArgs()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddResourceMonitoring()
            .BuildServiceProvider();

        var trackerService = provider.GetRequiredService<IResourceMonitor>();

        Assert.NotNull(trackerService);
        Assert.IsType<ResourceMonitorService>(trackerService);
        Assert.IsAssignableFrom<IResourceMonitor>(trackerService);
    }

    [ConditionalFact]
    public void AddsResourceMonitoringService_AsHostedService()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddResourceMonitoring(builder =>
            {
                builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                builder.AddPublisher<EmptyPublisher>();
            })
            .BuildServiceProvider();

        var allHostedServices = provider.GetServices<IHostedService>();
        var trackerService = allHostedServices.Single(s => s is ResourceMonitorService);

        Assert.NotNull(trackerService);
        Assert.IsType<ResourceMonitorService>(trackerService);
        Assert.IsAssignableFrom<IResourceMonitor>(trackerService);
    }

    [ConditionalFact]
    public void ConfigureResourceUtilization_InitializeTrackerProperly()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services.AddResourceMonitoring(builder =>
                {
                    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                });
            })
            .Build();

        var tracker = host.Services.GetService<IResourceMonitor>();
        var options = host.Services.GetService<IOptions<ResourceMonitoringOptions>>();
        var provider = host.Services.GetService<ISnapshotProvider>();
        var publisher = host.Services.GetService<IResourceUtilizationPublisher>();

        Assert.NotNull(tracker);
        Assert.NotNull(options);
        Assert.NotNull(provider);
        Assert.NotNull(publisher);
    }

    [ConditionalFact]
    public void ConfigureMonitor_GivenOptionsDelegate_InitializeTrackerWithOptionsProperly()
    {
        const int SamplingWindowValue = 3;
        const int CalculationPeriodValue = 2;

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services.AddResourceMonitoring(builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                    builder.ConfigureMonitor(options =>
                    {
                        options.CollectionWindow = TimeSpan.FromSeconds(SamplingWindowValue);
                        options.PublishingWindow = TimeSpan.FromSeconds(CalculationPeriodValue);
                    });
                });
            }).Build();

        var options = host.Services.GetService<IOptions<ResourceMonitoringOptions>>();

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromSeconds(SamplingWindowValue), options!.Value.CollectionWindow);
        Assert.Equal(TimeSpan.FromSeconds(CalculationPeriodValue), options!.Value.PublishingWindow);
    }

    [ConditionalFact]
    public void ConfigureMonitor_GivenIConfigurationSection_InitializeTrackerWithOptionsProperly()
    {
        const int SamplingWindowValue = 3;
        const int CalculationPeriod = 2;
        const int SamplingPeriodValue = 1;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.CollectionWindow)}"]
                    = TimeSpan.FromSeconds(SamplingWindowValue).ToString(),
                [$"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.SamplingInterval)}"]
                    = TimeSpan.FromSeconds(SamplingPeriodValue).ToString(),
                [$"{nameof(ResourceMonitoringOptions)}:{nameof(ResourceMonitoringOptions.PublishingWindow)}"]
                        = TimeSpan.FromSeconds(CalculationPeriod).ToString()
            })
            .Build();

        var configurationSection = config
            .GetSection(nameof(ResourceMonitoringOptions));

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services.AddResourceMonitoring(builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                    builder.ConfigureMonitor(configurationSection);
                });
            }).Build();

        var options = host.Services.GetService<IOptions<ResourceMonitoringOptions>>();

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromSeconds(SamplingWindowValue), options!.Value.CollectionWindow);
        Assert.Equal(TimeSpan.FromSeconds(SamplingPeriodValue), options!.Value.SamplingInterval);
        Assert.Equal(TimeSpan.FromSeconds(CalculationPeriod), options!.Value.PublishingWindow);
    }

    [ConditionalFact]
    public void Registering_Resource_Utilization_Adds_Only_One_Object_Of_Type_ResourceUtilizationService_To_DI_Container()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services.AddResourceMonitoring(builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                });
            }).Build();

        var trackers = host.Services.GetServices<IResourceMonitor>().ToArray();
        var backgrounds = host.Services.GetServices<IHostedService>().Where(x => x is ResourceMonitorService).ToArray();

        var tracker = Assert.Single(trackers);
        var background = Assert.Single(backgrounds);
        Assert.IsAssignableFrom<ResourceMonitorService>(tracker);
        Assert.IsAssignableFrom<ResourceMonitorService>(background);
        Assert.Same(tracker as ResourceMonitorService, background as ResourceMonitorService);
    }
}

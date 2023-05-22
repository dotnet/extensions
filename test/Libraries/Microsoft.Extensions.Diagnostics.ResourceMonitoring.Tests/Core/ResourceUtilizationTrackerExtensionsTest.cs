// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class ResourceUtilizationTrackerExtensionsTest
{
    [Fact]
    public void AddResourceUtilization_AddsResourceUtilizationTrackerService_ToServicesCollection()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddResourceUtilization(builder =>
            {
                builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                builder.AddPublisher<EmptyPublisher>();
            })
            .BuildServiceProvider();

        var trackerService = provider.GetRequiredService<IResourceUtilizationTracker>();

        Assert.NotNull(trackerService);
        Assert.IsType<ResourceUtilizationTrackerService>(trackerService);
        Assert.IsAssignableFrom<IResourceUtilizationTracker>(trackerService);
    }

    [Fact]
    public void AddResourceUtilization_AddsResourceUtilizationTrackerService_AsHostedService()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddResourceUtilization(builder =>
            {
                builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                builder.AddPublisher<EmptyPublisher>();
            })
            .BuildServiceProvider();

        var allHostedServices = provider.GetServices<IHostedService>();
        var trackerService = allHostedServices.Single(s => s is ResourceUtilizationTrackerService);

        Assert.NotNull(trackerService);
        Assert.IsType<ResourceUtilizationTrackerService>(trackerService);
        Assert.IsAssignableFrom<IResourceUtilizationTracker>(trackerService);
    }

    [Fact]
    public void ConfigureResourceUtilization_InitializeTrackerProperly()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureResourceUtilization(
            builder =>
            {
                builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
                builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                builder.AddPublisher<EmptyPublisher>();
            })
            .Build();

        var tracker = host.Services.GetService<IResourceUtilizationTracker>();
        var options = host.Services.GetService<IOptions<ResourceUtilizationTrackerOptions>>();
        var provider = host.Services.GetService<ISnapshotProvider>();
        var publisher = host.Services.GetService<IResourceUtilizationPublisher>();

        Assert.NotNull(tracker);
        Assert.NotNull(options);
        Assert.NotNull(provider);
        Assert.NotNull(publisher);
    }

    [Fact]
    public void ConfigureTracker_GivenOptionsDelegate_InitializeTrackerWithOptionsProperly()
    {
        const int SamplingWindowValue = 3;
        const int CalculationPeriodValue = 2;

        using var host = FakeHost.CreateBuilder()
            .ConfigureResourceUtilization(
                builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                    builder.ConfigureTracker(options =>
                    {
                        options.CollectionWindow = TimeSpan.FromSeconds(SamplingWindowValue);
                        options.CalculationPeriod = TimeSpan.FromSeconds(CalculationPeriodValue);
                    });
                })
            .Build();

        var options = host.Services.GetService<IOptions<ResourceUtilizationTrackerOptions>>();

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromSeconds(SamplingWindowValue), options!.Value.CollectionWindow);
        Assert.Equal(TimeSpan.FromSeconds(CalculationPeriodValue), options!.Value.CalculationPeriod);
    }

    [Fact]
    public void ConfigureTracker_GivenIConfigurationSection_InitializeTrackerWithOptionsProperly()
    {
        const int SamplingWindowValue = 3;
        const int CalculationPeriod = 2;
        const int SamplingPeriodValue = 1;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(ResourceUtilizationTrackerOptions)}:{nameof(ResourceUtilizationTrackerOptions.CollectionWindow)}"]
                    = TimeSpan.FromSeconds(SamplingWindowValue).ToString(),
                [$"{nameof(ResourceUtilizationTrackerOptions)}:{nameof(ResourceUtilizationTrackerOptions.SamplingInterval)}"]
                    = TimeSpan.FromSeconds(SamplingPeriodValue).ToString(),
                [$"{nameof(ResourceUtilizationTrackerOptions)}:{nameof(ResourceUtilizationTrackerOptions.CalculationPeriod)}"]
                        = TimeSpan.FromSeconds(CalculationPeriod).ToString()
            })
            .Build();

        var configurationSection = config
            .GetSection(nameof(ResourceUtilizationTrackerOptions));

        using var host = FakeHost.CreateBuilder()
            .ConfigureResourceUtilization(
                builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                    builder.ConfigureTracker(configurationSection);
                })
            .Build();

        var options = host.Services.GetService<IOptions<ResourceUtilizationTrackerOptions>>();

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromSeconds(SamplingWindowValue), options!.Value.CollectionWindow);
        Assert.Equal(TimeSpan.FromSeconds(SamplingPeriodValue), options!.Value.SamplingInterval);
        Assert.Equal(TimeSpan.FromSeconds(CalculationPeriod), options!.Value.CalculationPeriod);
    }

    [Fact]
    public void Registering_Resource_Utilization_Adds_Only_One_Object_Of_Type_ResourceUtilizationService_To_DI_Container()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureResourceUtilization(
                builder =>
                {
                    builder.Services.AddSingleton<ISnapshotProvider, FakeProvider>();
                    builder.AddPublisher<EmptyPublisher>();
                })
            .Build();

        var trackers = host.Services.GetServices<IResourceUtilizationTracker>().ToArray();
        var backgrounds = host.Services.GetServices<IHostedService>().Where(x => x is ResourceUtilizationTrackerService).ToArray();

        var tracker = Assert.Single(trackers);
        var background = Assert.Single(backgrounds);
        Assert.IsAssignableFrom<ResourceUtilizationTrackerService>(tracker);
        Assert.IsAssignableFrom<ResourceUtilizationTrackerService>(background);
        Assert.Same(tracker as ResourceUtilizationTrackerService, background as ResourceUtilizationTrackerService);
    }
}

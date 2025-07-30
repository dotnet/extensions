// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

[OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Not supported on MacOs.")]
public sealed class ResourceMonitoringBuilderTests
{
    [ConditionalFact(Skip = "Not supported on MacOs.")]
    public void AddPublisher_CalledOnce_AddsSinglePublisherToServiceCollection()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddResourceMonitoring(builder =>
            {
                builder.AddPublisher<EmptyPublisher>();
            })
            .BuildServiceProvider();

        var publisher = provider.GetRequiredService<IResourceUtilizationPublisher>();
        var publishersArray = provider.GetServices<IResourceUtilizationPublisher>();

        Assert.NotNull(publisher);
        Assert.IsType<EmptyPublisher>(publisher);
        Assert.NotNull(publishersArray);
        Assert.Single(publishersArray);
        Assert.IsAssignableFrom<EmptyPublisher>(publishersArray.First());
    }

    [ConditionalFact]
    public void AddPublisher_CalledMultipleTimes_AddsMultiplePublishersToServiceCollection()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddResourceMonitoring(builder =>
            {
                builder
                    .AddPublisher<EmptyPublisher>()
                    .AddPublisher<AnotherPublisher>();
            })
            .BuildServiceProvider();

        var publishersArray = provider.GetServices<IResourceUtilizationPublisher>();

        Assert.NotNull(publishersArray);
        Assert.Equal(2, publishersArray.Count());
        Assert.IsAssignableFrom<EmptyPublisher>(publishersArray.First());
        Assert.IsAssignableFrom<AnotherPublisher>(publishersArray.Last());
    }

    [ConditionalFact]
    public void AddResourceMonitoring_WithoutConfigureMonitor_CannotResolveSnapshotProviderRequiringOptions()
    {
        // Arrange - Try to register WindowsSnapshotProvider which requires IOptions<ResourceMonitoringOptions>
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IMeterFactory>(sp => new FakeMeterFactory())
            .AddResourceMonitoring()
            .AddSingleton<WindowsSnapshotProvider>(); // ← This will fail to resolve

        using var provider = services.BuildServiceProvider();

        // Act & Assert - Should throw because IOptions<ResourceMonitoringOptions> is not registered
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<WindowsSnapshotProvider>());

        Assert.Contains("IOptions", exception.Message);
    }

    [ConditionalFact]
    public void AddResourceMonitoring_WithManualOptionsConfiguration_AllowsSnapshotProviderResolution()
    {
        // Arrange - Manually register options to fix the issue
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IMeterFactory>(sp => new FakeMeterFactory())
            .AddResourceMonitoring()
            .Configure<ResourceMonitoringOptions>(options => { }) // ← Manual fix
            .AddSingleton<WindowsSnapshotProvider>();

        using var provider = services.BuildServiceProvider();

        // Act & Assert - Should now work
        var snapshotProvider = provider.GetRequiredService<WindowsSnapshotProvider>();

        Assert.NotNull(snapshotProvider);
        Assert.NotNull(snapshotProvider.Resources);
    }

    internal sealed class FakeMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options)
        {
            return new Meter(options.Name, options.Version);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}

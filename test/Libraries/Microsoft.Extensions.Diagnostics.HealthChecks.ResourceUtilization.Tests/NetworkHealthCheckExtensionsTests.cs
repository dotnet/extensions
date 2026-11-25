// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class NetworkHealthCheckExtensionsTests
{
    [Fact]
    public async Task AddNetworkHealthCheck()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        HealthReport report = await service.CheckHealthAsync();

        Assert.True(report.Entries.TryGetValue("network connections", out HealthReportEntry entry));
        Assert.Equal(HealthStatus.Healthy, entry.Status);
        Assert.True(entry.Data.ContainsKey("EstablishedConnections"));
        Assert.True(entry.Data.ContainsKey("SynReceivedConnections"));
    }

    [Fact]
    public async Task AddNetworkHealthCheck_WithTags()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck("test");

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();

        HealthReport taggedReport = await service.CheckHealthAsync(registration => registration.Tags.Contains("test"));
        Assert.True(taggedReport.Entries.ContainsKey("network connections"));

        HealthReport untaggedReport = await service.CheckHealthAsync(registration => registration.Tags.Contains("other"));
        Assert.Empty(untaggedReport.Entries);
    }

    [Fact]
    public async Task AddNetworkHealthCheck_WithTagsEnumerable()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck(new List<string> { "test" });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();

        HealthReport report = await service.CheckHealthAsync(registration => registration.Tags.Contains("test"));
        Assert.True(report.Entries.ContainsKey("network connections"));
    }

    [Fact]
    public async Task AddNetworkHealthCheck_WithAction()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck(o =>
            {
                o.EstablishedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 500, UnhealthyCount = 1000 };
                o.SynReceivedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 500, UnhealthyCount = 1000 };
            });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        HealthReport report = await service.CheckHealthAsync();

        Assert.True(report.Entries.TryGetValue("network connections", out HealthReportEntry entry));
        Assert.Equal(HealthStatus.Healthy, entry.Status);
    }

    [Fact]
    public async Task AddNetworkHealthCheck_WithConfigurationSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NetworkUtilizationHealthCheckOptions:EstablishedConnectionsThresholds:DegradedCount"] = "500",
                ["NetworkUtilizationHealthCheckOptions:EstablishedConnectionsThresholds:UnhealthyCount"] = "1000",
                ["NetworkUtilizationHealthCheckOptions:SynReceivedConnectionsThresholds:DegradedCount"] = "500",
                ["NetworkUtilizationHealthCheckOptions:SynReceivedConnectionsThresholds:UnhealthyCount"] = "1000",
            })
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck(configuration.GetSection(nameof(NetworkUtilizationHealthCheckOptions)));

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        HealthCheckService service = serviceProvider.GetRequiredService<HealthCheckService>();
        HealthReport report = await service.CheckHealthAsync();

        Assert.True(report.Entries.TryGetValue("network connections", out HealthReportEntry entry));
        Assert.Equal(HealthStatus.Healthy, entry.Status);

        var options = serviceProvider.GetRequiredService<IOptions<NetworkUtilizationHealthCheckOptions>>().Value;
        Assert.Equal(500, options.EstablishedConnectionsThresholds.DegradedCount);
        Assert.Equal(1000, options.EstablishedConnectionsThresholds.UnhealthyCount);
        Assert.Equal(500, options.SynReceivedConnectionsThresholds.DegradedCount);
        Assert.Equal(1000, options.SynReceivedConnectionsThresholds.UnhealthyCount);
    }

    [Fact]
    public void AddNetworkHealthCheck_RegistersInternalResourceMonitoring()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddLogging()
            .AddHealthChecks()
            .AddNetworkUtilizationHealthCheck();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IResourceMonitor? resourceMonitor = serviceProvider.GetService<IResourceMonitor>();
        Assert.NotNull(resourceMonitor);

        var resourceMonitoringOptions = serviceProvider.GetRequiredService<IOptions<ResourceMonitoringOptions>>().Value;
        Assert.NotNull(resourceMonitoringOptions);
    }

    [Fact]
    public void NetworkUtilizationHealthCheckOptionsValidator_RejectsNegativeThresholds()
    {
        var validator = new NetworkUtilizationHealthCheckOptionsValidator();
        ValidateOptionsResult result = validator.Validate("test", new NetworkUtilizationHealthCheckOptions
        {
            EstablishedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = -1 },
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void NetworkUtilizationHealthCheckOptionsValidator_AcceptsValidThresholds()
    {
        var validator = new NetworkUtilizationHealthCheckOptionsValidator();
        ValidateOptionsResult result = validator.Validate("test", new NetworkUtilizationHealthCheckOptions
        {
            EstablishedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 500, UnhealthyCount = 1000 },
            SynReceivedConnectionsThresholds = new ResourceCountThresholds { DegradedCount = 500, UnhealthyCount = 1000 },
        });

        Assert.True(result.Succeeded);
    }
}

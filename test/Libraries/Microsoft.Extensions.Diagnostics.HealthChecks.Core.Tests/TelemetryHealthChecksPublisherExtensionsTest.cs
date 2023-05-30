// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Core.Tests;

public class TelemetryHealthChecksPublisherExtensionsTest
{
    [Fact]
    public void AddHealthCheckTelemetryTest()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddLogging()
            .AddSingleton<ILoggerFactory, LoggerFactory>()
            .AddTelemetryHealthCheckPublisher();

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is TelemetryHealthCheckPublisher);
        }
    }

    [Fact]
    public void AddHealthCheckTelemetryTest_WithAction()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddLogging()
            .AddSingleton<ILoggerFactory, LoggerFactory>()
            .AddTelemetryHealthCheckPublisher(o =>
            {
                o.LogOnlyUnhealthy = true;
            });

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is TelemetryHealthCheckPublisher);
        }
    }

    [Fact]
    public void AddHealthCheckTelemetryTest_WithConfigurationSection()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddLogging()
            .AddSingleton<ILoggerFactory, LoggerFactory>()
            .AddTelemetryHealthCheckPublisher(SetupTelemetryHealthCheckPublisherConfiguration("true")
                .GetSection(nameof(TelemetryHealthCheckPublisherOptions)));

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is TelemetryHealthCheckPublisher);
        }
    }

    private static IConfiguration SetupTelemetryHealthCheckPublisherConfiguration(string logOnlyUnhealthy)
    {
        TelemetryHealthCheckPublisherOptions telemetryHealthCheckPublisherOptions;

        var configurationDict = new Dictionary<string, string?>
            {
               {
                    $"{nameof(telemetryHealthCheckPublisherOptions)}:{nameof(telemetryHealthCheckPublisherOptions.LogOnlyUnhealthy)}",
                    logOnlyUnhealthy
               }
            };

        return new ConfigurationBuilder().AddInMemoryCollection(configurationDict).Build();
    }
}

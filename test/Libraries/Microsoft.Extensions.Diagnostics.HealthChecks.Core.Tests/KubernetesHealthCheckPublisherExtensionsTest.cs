// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Core.Tests;

public class KubernetesHealthCheckPublisherExtensionsTest
{
    [Fact]
    public void AddKubernetesHealthCheckPublisherTest()
    {
        var serviceCollection = new ServiceCollection();

        _ = serviceCollection.Configure<HealthCheckPublisherOptions>(o => { });
        serviceCollection.AddKubernetesHealthCheckPublisher();

        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is KubernetesHealthCheckPublisher);
        }
    }

    [Fact]
    public void AddKubernetesHealthCheckPublisherTest_WithAction()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddKubernetesHealthCheckPublisher(o =>
        {
            o.TcpPort = 2305;
            o.MaxPendingConnections = 10;
        });

        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is KubernetesHealthCheckPublisher);
        }
    }

    [Fact]
    public void AddKubernetesHealthCheckPublisherTest_WithConfigurationSection()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddKubernetesHealthCheckPublisher(SetupKubernetesHealthCheckPublisherConfiguration("2305", "10")
            .GetSection(nameof(KubernetesHealthCheckPublisherOptions)));

        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var publishers = serviceProvider.GetRequiredService<IEnumerable<IHealthCheckPublisher>>();

        Assert.Single(publishers);
        foreach (var p in publishers)
        {
            Assert.True(p is KubernetesHealthCheckPublisher);
        }
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddKubernetesHealthCheckPublisher((Action<KubernetesHealthCheckPublisherOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddKubernetesHealthCheckPublisher((IConfigurationSection)null!));
    }

    private static IConfiguration SetupKubernetesHealthCheckPublisherConfiguration(
        string tcpPort,
        string maxLengthOfPendingConnectionsQueue)
    {
        KubernetesHealthCheckPublisherOptions kubernetesHealthCheckPublisherOptions;

        var configurationDict = new Dictionary<string, string?>
            {
               {
                    $"{nameof(KubernetesHealthCheckPublisherOptions)}:{nameof(kubernetesHealthCheckPublisherOptions.TcpPort)}",
                    tcpPort
               },
               {
                    $"{nameof(KubernetesHealthCheckPublisherOptions)}:{nameof(kubernetesHealthCheckPublisherOptions.MaxPendingConnections)}",
                    maxLengthOfPendingConnectionsQueue
               }
            };

        return new ConfigurationBuilder().AddInMemoryCollection(configurationDict).Build();
    }
}

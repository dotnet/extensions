// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

public class KubernetesResourceQuotasServiceCollectionExtensionsTests
{
    [Fact]
    public void AddKubernetesResourceMonitoring_WithoutConfiguration_RegistersServicesCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB  
        ulong requestsCpu = 1_000; // 1 core in millicores
        using TestKubernetesEnvironmentSetup setup = new();
        setup.SetupKubernetesEnvironment(prefix: string.Empty, limitsMemory, limitsCpu, requestsMemory, requestsCpu);

        // Act
        services.AddKubernetesResourceMonitoring();
        services
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ResourceQuotaProvider? resourceQuotaProvider = serviceProvider.GetService<ResourceQuotaProvider>();

        Assert.NotNull(resourceQuotaProvider);
        Assert.IsType<KubernetesResourceQuotaProvider>(resourceQuotaProvider);
        Assert.NotNull(serviceProvider.GetService<IResourceMonitor>());
    }

    [Fact]
    public void AddKubernetesResourceMonitoring_WithConfiguration_RegistersServicesCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB  
        ulong requestsCpu = 1_000; // 1 core in millicores
        using TestKubernetesEnvironmentSetup setup = new();
        setup.SetupKubernetesEnvironment("TEST_CLUSTER_", limitsMemory, limitsCpu, requestsMemory, requestsCpu);

        // Act
        services.AddKubernetesResourceMonitoring("TEST_CLUSTER_");
        services
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ResourceQuotaProvider? resourceQuotaProvider = serviceProvider.GetService<ResourceQuotaProvider>();

        Assert.NotNull(resourceQuotaProvider);
        Assert.IsType<KubernetesResourceQuotaProvider>(resourceQuotaProvider);
        Assert.NotNull(serviceProvider.GetService<IResourceMonitor>());
    }

    [Fact]
    public void AddKubernetesResourceMonitoring_WithMinimalConfiguration_RegistersServicesCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        using TestKubernetesEnvironmentSetup setup = new();
        setup.SetupMinimalKubernetesEnvironmentWithoutRequests("TEST_CLUSTER_");

        // Act
        services.AddKubernetesResourceMonitoring("TEST_CLUSTER_");
        services
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ResourceQuotaProvider? resourceQuotaProvider = serviceProvider.GetService<ResourceQuotaProvider>();

        Assert.NotNull(resourceQuotaProvider);
        Assert.IsType<KubernetesResourceQuotaProvider>(resourceQuotaProvider);
        Assert.NotNull(serviceProvider.GetService<IResourceMonitor>());
    }

#pragma warning disable CS0618 // Type or member is obsolete - ISnapshotProvider is marked obsolete but still available for testing
    [Fact]
    public void AddKubernetesResourceMonitoring_RegistersISnapshotProvider()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB  
        ulong requestsCpu = 1_000; // 1 core in millicores
        using TestKubernetesEnvironmentSetup setup = new();
        setup.SetupKubernetesEnvironment(prefix: string.Empty, limitsMemory, limitsCpu, requestsMemory, requestsCpu);

        // Act
        services.AddKubernetesResourceMonitoring();
        services
            .AddLogging()
            .AddSingleton<TimeProvider>(TimeProvider.System);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ISnapshotProvider? snapshotProvider = serviceProvider.GetService<ISnapshotProvider>();
        Assert.NotNull(snapshotProvider);

        Assert.NotEqual(default, snapshotProvider.Resources);

        var snapshot = snapshotProvider.GetSnapshot();
        Assert.NotEqual(default, snapshot);
    }
#pragma warning restore CS0618
}

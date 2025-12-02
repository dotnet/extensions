// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

[CollectionDefinition("EnvironmentVariableTests", DisableParallelization = true)]
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
    public void AddKubernetesResourceMonitoring_WithValidEnvironmentData_RegistersISnapshotProvider()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        const string TestPrefix = "K8S_TEST_";
        ulong limitsMemory = 2_147_483_648;
        ulong limitsCpu = 2_000;
        ulong requestsMemory = 1_073_741_824;
        ulong requestsCpu = 1_000;

        using TestKubernetesEnvironmentSetup setup = new();
        setup.SetupKubernetesEnvironment(prefix: TestPrefix, limitsMemory, limitsCpu, requestsMemory, requestsCpu);

        // Act
        services.AddKubernetesResourceMonitoring(TestPrefix);
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

        var resourceQuotaProvider = serviceProvider.GetRequiredService<ResourceQuotaProvider>();
        Assert.IsType<KubernetesResourceQuotaProvider>(resourceQuotaProvider);

        var quota = resourceQuotaProvider.GetResourceQuota();
        Assert.Equal(2.0, quota.MaxCpuInCores);
        Assert.Equal(1.0, quota.BaselineCpuInCores);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes);
        Assert.Equal(requestsMemory, quota.BaselineMemoryInBytes);
    }

    [Fact]
    public void LinuxUtilizationProvider_ThrowsWhenLimitsNotFound()
    {
        using var environmentSetup = new TestKubernetesEnvironmentSetup();
        environmentSetup.SetEnvironmentVariable("TEST_REQUESTS_MEMORY", "1000");
        environmentSetup.SetEnvironmentVariable("TEST_REQUESTS_CPU", "1000");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            KubernetesMetadata.FromEnvironmentVariables("TEST_"));

        Assert.Contains("LIMITS_MEMORY", exception.Message);
        Assert.Contains("is required and cannot be zero or missing", exception.Message);
    }
#pragma warning restore CS0618
}

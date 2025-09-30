// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

public class KubernetesResourceQuotasServiceCollectionExtensionsTests
{
    [Fact]
    public void AddKubernetesResourceMonitoring_WithoutConfiguration_RegistersServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKubernetesResourceMonitoring();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        IResourceQuotaProvider? resourceQuotaProvider = serviceProvider.GetService<IResourceQuotaProvider>();
        Assert.NotNull(resourceQuotaProvider);
        Assert.IsType<KubernetesResourceQuotaProvider>(resourceQuotaProvider);
        Assert.NotNull(serviceProvider.GetService<KubernetesMetadata>());
        Assert.NotNull(serviceProvider.GetService<IResourceMonitor>());
    }
}

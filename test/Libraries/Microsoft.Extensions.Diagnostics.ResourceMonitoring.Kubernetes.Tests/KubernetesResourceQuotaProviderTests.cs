// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

public class KubernetesResourceQuotaProviderTests
{
    [Fact]
    public void Constructor_WithValidMetadata_DoesNotThrow()
    {
        // Arrange
        var metadata = CreateKubernetesMetadata(
            limitsMemory: 2_147_483_648,
            limitsCpu: 2_000,
            requestsMemory: 1_073_741_824,
            requestsCpu: 1_000);

        // Act & Assert
        var exception = Record.Exception(() => new KubernetesResourceQuotaProvider(metadata));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KubernetesResourceQuotaProvider(null!));
    }

    [Fact]
    public void GetResourceQuota_WithValidRequestsAndLimits_ReturnsCorrectQuota()
    {
        // Arrange
        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB
        ulong requestsCpu = 1_000; // 1 core in millicores

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes);
        Assert.Equal(2.0, quota.MaxCpuInCores); // 2000 millicores = 2 cores
        Assert.Equal(requestsMemory, quota.BaselineMemoryInBytes);
        Assert.Equal(1.0, quota.BaselineCpuInCores); // 1000 millicores = 1 core
    }

    [Fact]
    public void GetResourceQuota_WithZeroCpuRequests_UsesLimitAsBaseline()
    {
        // Arrange
        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB
        ulong requestsCpu = 0; // No CPU requests

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes);
        Assert.Equal(2.0, quota.MaxCpuInCores); // 2000 millicores = 2 cores
        Assert.Equal(requestsMemory, quota.BaselineMemoryInBytes);
        Assert.Equal(2.0, quota.BaselineCpuInCores); // Should fallback to limit (2 cores)
    }

    [Fact]
    public void GetResourceQuota_WithZeroMemoryRequests_UsesLimitAsBaseline()
    {
        // Arrange
        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores in millicores
        ulong requestsMemory = 0; // No memory requests
        ulong requestsCpu = 1_000; // 1 core in millicores

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes);
        Assert.Equal(2.0, quota.MaxCpuInCores); // 2000 millicores = 2 cores
        Assert.Equal(limitsMemory, quota.BaselineMemoryInBytes); // Should fallback to limit (2GB)
        Assert.Equal(1.0, quota.BaselineCpuInCores); // 1000 millicores = 1 core
    }

    [Fact]
    public void GetResourceQuota_WithZeroRequestsAndLimits_ReturnsZeroValues()
    {
        // Arrange
        var metadata = CreateKubernetesMetadata(0, 0, 0, 0);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(0UL, quota.MaxMemoryInBytes);
        Assert.Equal(0.0, quota.MaxCpuInCores);
        Assert.Equal(0UL, quota.BaselineMemoryInBytes);
        Assert.Equal(0.0, quota.BaselineCpuInCores);
    }

    [Fact]
    public void GetResourceQuota_WithMaximumValues_HandlesCorrectly()
    {
        // Arrange
        ulong maxMemory = ulong.MaxValue;
        ulong maxCpu = ulong.MaxValue;
        ulong requestsMemory = ulong.MaxValue / 2;
        ulong requestsCpu = ulong.MaxValue / 2;

        var metadata = CreateKubernetesMetadata(maxMemory, maxCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(maxMemory, quota.MaxMemoryInBytes);
        Assert.Equal(maxCpu / 1000.0, quota.MaxCpuInCores);
        Assert.Equal(requestsMemory, quota.BaselineMemoryInBytes);
        Assert.Equal(requestsCpu / 1000.0, quota.BaselineCpuInCores);
    }

    [Theory]
    [InlineData(2_000UL, 2.0)] // 2 cores
    [InlineData(1_000UL, 1.0)] // 1 core
    [InlineData(1_500UL, 1.5)] // 1.5 cores
    [InlineData(500UL, 0.5)] // 0.5 cores
    [InlineData(100UL, 0.1)] // 0.1 cores
    [InlineData(0UL, 0.0)] // 0 cores
    public void GetResourceQuota_ConvertsMillicoresToCoresCorrectly(ulong millicores, double expectedCores)
    {
        // Arrange
        var metadata = CreateKubernetesMetadata(
            limitsMemory: 1_073_741_824, // 1GB
            limitsCpu: millicores,
            requestsMemory: 536_870_912, // 512MB
            requestsCpu: millicores / 2);

        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.Equal(expectedCores, quota.MaxCpuInCores, precision: 10);
        Assert.Equal(expectedCores / 2, quota.BaselineCpuInCores, precision: 10);
    }

    [Fact]
    public void GetResourceQuota_CallMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var metadata = CreateKubernetesMetadata(
            limitsMemory: 2_147_483_648,
            limitsCpu: 2_000,
            requestsMemory: 1_073_741_824,
            requestsCpu: 1_000);

        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota1 = provider.GetResourceQuota();
        var quota2 = provider.GetResourceQuota();
        var quota3 = provider.GetResourceQuota();

        // Assert
        Assert.Equal(quota1.MaxMemoryInBytes, quota2.MaxMemoryInBytes);
        Assert.Equal(quota1.MaxCpuInCores, quota2.MaxCpuInCores);
        Assert.Equal(quota1.BaselineMemoryInBytes, quota2.BaselineMemoryInBytes);
        Assert.Equal(quota1.BaselineCpuInCores, quota2.BaselineCpuInCores);

        Assert.Equal(quota2.MaxMemoryInBytes, quota3.MaxMemoryInBytes);
        Assert.Equal(quota2.MaxCpuInCores, quota3.MaxCpuInCores);
        Assert.Equal(quota2.BaselineMemoryInBytes, quota3.BaselineMemoryInBytes);
        Assert.Equal(quota2.BaselineCpuInCores, quota3.BaselineCpuInCores);
    }

    [Fact]
    public void GetResourceQuota_WithRequestsHigherThanLimits_ReturnsActualValues()
    {
        // Arrange - This scenario shouldn't normally happen in Kubernetes, but testing edge case
        ulong limitsMemory = 1_073_741_824; // 1GB
        ulong limitsCpu = 1_000; // 1 core
        ulong requestsMemory = 2_147_483_648; // 2GB (higher than limit)
        ulong requestsCpu = 2_000; // 2 cores (higher than limit)

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes); // Limits are preserved
        Assert.Equal(1.0, quota.MaxCpuInCores);
        Assert.Equal(requestsMemory, quota.BaselineMemoryInBytes); // Requests are preserved as-is
        Assert.Equal(2.0, quota.BaselineCpuInCores);
    }

    [Fact]
    public void GetResourceQuota_WithTypicalKubernetesValues_ReturnsExpectedQuota()
    {
        // Arrange - Typical Kubernetes pod resource configuration
        ulong limitsMemory = 4_294_967_296; // 4GB
        ulong limitsCpu = 4_000; // 4 cores in millicores
        ulong requestsMemory = 1_073_741_824; // 1GB
        ulong requestsCpu = 500; // 0.5 cores in millicores

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(4_294_967_296UL, quota.MaxMemoryInBytes); // 4GB
        Assert.Equal(4.0, quota.MaxCpuInCores);
        Assert.Equal(1_073_741_824UL, quota.BaselineMemoryInBytes); // 1GB
        Assert.Equal(0.5, quota.BaselineCpuInCores);
    }

    [Theory]
    [InlineData(0UL, 1_000UL)] // Only CPU requests missing
    [InlineData(1_000UL, 0UL)] // Only memory requests missing
    public void GetResourceQuota_WithPartialRequests_AppliesFallbackLogicCorrectly(ulong requestsMemory, ulong requestsCpu)
    {
        // Arrange
        ulong limitsMemory = 2_147_483_648; // 2GB
        ulong limitsCpu = 2_000; // 2 cores

        var metadata = CreateKubernetesMetadata(limitsMemory, limitsCpu, requestsMemory, requestsCpu);
        var provider = new KubernetesResourceQuotaProvider(metadata);

        // Act
        var quota = provider.GetResourceQuota();

        // Assert
        Assert.NotNull(quota);
        Assert.Equal(limitsMemory, quota.MaxMemoryInBytes);
        Assert.Equal(2.0, quota.MaxCpuInCores);

        // Check fallback logic
        ulong expectedBaselineMemory = requestsMemory == 0 ? limitsMemory : requestsMemory;
        double expectedBaselineCpu = requestsCpu == 0 ? 2.0 : requestsCpu / 1000.0;

        Assert.Equal(expectedBaselineMemory, quota.BaselineMemoryInBytes);
        Assert.Equal(expectedBaselineCpu, quota.BaselineCpuInCores);
    }

    /// <summary>
    /// Creates a KubernetesMetadata instance with the specified values for testing.
    /// </summary>
    private static KubernetesMetadata CreateKubernetesMetadata(
        ulong limitsMemory,
        ulong limitsCpu,
        ulong requestsMemory,
        ulong requestsCpu)
    {
        return new KubernetesMetadata
        {
            LimitsMemory = limitsMemory,
            LimitsCpu = limitsCpu,
            RequestsMemory = requestsMemory,
            RequestsCpu = requestsCpu
        };
    }
}

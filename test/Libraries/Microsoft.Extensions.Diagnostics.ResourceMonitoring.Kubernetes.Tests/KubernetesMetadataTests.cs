// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

public class KubernetesMetadataTests
{
    [Fact]
    public void Constructor_WithValidPrefix_SetsPrefix()
    {
        // Arrange & Act
        var metadata = new KubernetesMetadata("TEST_PREFIX_");

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void Constructor_WithEmptyPrefix_SetsEmptyPrefix()
    {
        // Arrange & Act
        var metadata = new KubernetesMetadata(string.Empty);

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void Constructor_WithNullPrefix_SetsNullPrefix()
    {
        // Arrange & Act
        var metadata = new KubernetesMetadata(null!);

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void Build_WithValidEnvironmentVariables_ReturnsCorrectValues()
    {
        // Arrange
        ulong expectedLimitsMemory = 2_147_483_648; // 2GB
        ulong expectedLimitsCpu = 2_000; // 2 cores in millicores
        ulong expectedRequestsMemory = 1_073_741_824; // 1GB
        ulong expectedRequestsCpu = 1_000; // 1 core in millicores

        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetupKubernetesEnvironment(
            prefix: string.Empty,
            limitsMemory: expectedLimitsMemory,
            limitsCpu: expectedLimitsCpu,
            requestsMemory: expectedRequestsMemory,
            requestsCpu: expectedRequestsCpu);

        var metadata = new KubernetesMetadata(string.Empty);

        try
        {
            // Act
            var result = metadata.Build();

            // Assert
            Assert.Same(metadata, result);
            Assert.Equal(expectedLimitsMemory, metadata.LimitsMemory);
            Assert.Equal(expectedLimitsCpu, metadata.LimitsCpu);
            Assert.Equal(expectedRequestsMemory, metadata.RequestsMemory);
            Assert.Equal(expectedRequestsCpu, metadata.RequestsCpu);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }

    [Fact]
    public void Build_WithCustomPrefix_ReadsCorrectEnvironmentVariables()
    {
        // Arrange
        const string CustomPrefix = "CUSTOM_CLUSTER_";
        ulong expectedLimitsMemory = 4_294_967_296; // 4GB
        ulong expectedLimitsCpu = 4_000; // 4 cores in millicores
        ulong expectedRequestsMemory = 2_147_483_648; // 2GB
        ulong expectedRequestsCpu = 2_000; // 2 cores in millicores

        using var environmentSetup = new TestKubernetesEnvironmentSetup();
        environmentSetup.SetupKubernetesEnvironment(
            prefix: CustomPrefix,
            limitsMemory: expectedLimitsMemory,
            limitsCpu: expectedLimitsCpu,
            requestsMemory: expectedRequestsMemory,
            requestsCpu: expectedRequestsCpu);

        var metadata = new KubernetesMetadata(CustomPrefix);

        // Act
        var result = metadata.Build();

        // Assert
        Assert.Same(metadata, result);
        Assert.Equal(expectedLimitsMemory, metadata.LimitsMemory);
        Assert.Equal(expectedLimitsCpu, metadata.LimitsCpu);
        Assert.Equal(expectedRequestsMemory, metadata.RequestsMemory);
        Assert.Equal(expectedRequestsCpu, metadata.RequestsCpu);
    }

    [Fact]
    public void Build_WithMissingEnvironmentVariables_SetsZeroValues()
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.ClearEnvironmentVariables();
        var metadata = new KubernetesMetadata("NONEXISTENT_PREFIX_");

        try
        {
            // Act
            var result = metadata.Build();

            // Assert
            Assert.Same(metadata, result);
            Assert.Equal(0UL, metadata.LimitsMemory);
            Assert.Equal(0UL, metadata.LimitsCpu);
            Assert.Equal(0UL, metadata.RequestsMemory);
            Assert.Equal(0UL, metadata.RequestsCpu);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Build_WithEmptyOrWhitespaceEnvironmentVariables_SetsZeroValues(string envValue)
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetEnvironmentVariable("LIMITS_MEMORY", envValue);
        tempSetup.SetEnvironmentVariable("LIMITS_CPU", envValue);
        tempSetup.SetEnvironmentVariable("REQUESTS_MEMORY", envValue);
        tempSetup.SetEnvironmentVariable("REQUESTS_CPU", envValue);

        var metadata = new KubernetesMetadata(string.Empty);

        try
        {
            // Act
            var result = metadata.Build();

            // Assert
            Assert.Equal(0UL, metadata.LimitsMemory);
            Assert.Equal(0UL, metadata.LimitsCpu);
            Assert.Equal(0UL, metadata.RequestsMemory);
            Assert.Equal(0UL, metadata.RequestsCpu);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }

    [Theory]
    [InlineData("LIMITS_MEMORY", "invalid")]
    [InlineData("LIMITS_CPU", "not-a-number")]
    [InlineData("REQUESTS_MEMORY", "-1")]
    [InlineData("REQUESTS_CPU", "1.5")]
    [InlineData("LIMITS_MEMORY", "18446744073709551616")] // ulong.MaxValue + 1
    public void Build_WithInvalidEnvironmentVariableValues_ThrowsInvalidOperationException(string varSuffix, string invalidValue)
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetEnvironmentVariable(varSuffix, invalidValue);
        var metadata = new KubernetesMetadata(string.Empty);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => metadata.Build());
            Assert.Contains(varSuffix, exception.Message);
            Assert.Contains(invalidValue, exception.Message);
            Assert.Contains("Expected a non-negative integer", exception.Message);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }

    [Fact]
    public void Build_WithPartialEnvironmentVariables_SetsAvailableValuesAndZerosForMissing()
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetEnvironmentVariable("LIMITS_MEMORY", "2147483648"); // 2GB
        tempSetup.SetEnvironmentVariable("LIMITS_CPU", "2000"); // 2 cores
        // Intentionally not setting REQUESTS_MEMORY and REQUESTS_CPU

        var metadata = new KubernetesMetadata(string.Empty);

        try
        {
            // Act
            metadata.Build();

            // Assert
            Assert.Equal(2_147_483_648UL, metadata.LimitsMemory);
            Assert.Equal(2_000UL, metadata.LimitsCpu);
            Assert.Equal(0UL, metadata.RequestsMemory); // Should be 0 for missing variables
            Assert.Equal(0UL, metadata.RequestsCpu); // Should be 0 for missing variables
        }
        finally
        {
            tempSetup.Dispose();
        }
    }

    [Fact]
    public void Build_WithDifferentPrefixes_ReadsDifferentVariables()
    {
        // Arrange
        const string Prefix1 = "CLUSTER1_";
        const string Prefix2 = "CLUSTER2_";

        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetEnvironmentVariable($"{Prefix1}LIMITS_MEMORY", "1073741824"); // 1GB
        tempSetup.SetEnvironmentVariable($"{Prefix1}LIMITS_CPU", "1000"); // 1 core
        tempSetup.SetEnvironmentVariable($"{Prefix2}LIMITS_MEMORY", "2147483648"); // 2GB
        tempSetup.SetEnvironmentVariable($"{Prefix2}LIMITS_CPU", "2000"); // 2 cores

        var metadata1 = new KubernetesMetadata(Prefix1);
        var metadata2 = new KubernetesMetadata(Prefix2);

        try
        {
            // Act
            metadata1.Build();
            metadata2.Build();

            // Assert
            Assert.Equal(1_073_741_824UL, metadata1.LimitsMemory);
            Assert.Equal(1_000UL, metadata1.LimitsCpu);
            Assert.Equal(2_147_483_648UL, metadata2.LimitsMemory);
            Assert.Equal(2_000UL, metadata2.LimitsCpu);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes.Tests;

public class KubernetesMetadataTests
{
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

        try
        {
            // Act
            var result = KubernetesMetadata.FromEnvironmentVariables(string.Empty);

            // Assert
            Assert.Equal(expectedLimitsMemory, result.LimitsMemory);
            Assert.Equal(expectedLimitsCpu, result.LimitsCpu);
            Assert.Equal(expectedRequestsMemory, result.RequestsMemory);
            Assert.Equal(expectedRequestsCpu, result.RequestsCpu);
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

        // Act
        var result = KubernetesMetadata.FromEnvironmentVariables(CustomPrefix);

        // Assert
        Assert.Equal(expectedLimitsMemory, result.LimitsMemory);
        Assert.Equal(expectedLimitsCpu, result.LimitsCpu);
        Assert.Equal(expectedRequestsMemory, result.RequestsMemory);
        Assert.Equal(expectedRequestsCpu, result.RequestsCpu);
    }

    [Fact]
    public void Build_WithMissingEnvironmentVariables_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.ClearEnvironmentVariables();

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                KubernetesMetadata.FromEnvironmentVariables("NONEXISTENT_PREFIX_"));

            Assert.Contains("LIMITS_MEMORY", exception.Message);
            Assert.Contains("is required and cannot be zero or missing", exception.Message);
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
    public void Build_WithEmptyOrWhitespaceEnvironmentVariables_ThrowsInvalidOperationException(string envValue)
    {
        // Arrange
        var tempSetup = new TestKubernetesEnvironmentSetup();
        tempSetup.SetEnvironmentVariable("LIMITS_MEMORY", envValue);
        tempSetup.SetEnvironmentVariable("LIMITS_CPU", envValue);
        tempSetup.SetEnvironmentVariable("REQUESTS_MEMORY", envValue);
        tempSetup.SetEnvironmentVariable("REQUESTS_CPU", envValue);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                KubernetesMetadata.FromEnvironmentVariables(string.Empty));

            Assert.Contains("LIMITS_MEMORY", exception.Message);
            Assert.Contains("is required and cannot be zero or missing", exception.Message);
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

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => KubernetesMetadata.FromEnvironmentVariables(string.Empty));
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

        try
        {
            // Act
            var result = KubernetesMetadata.FromEnvironmentVariables(string.Empty);

            // Assert
            Assert.Equal(2_147_483_648UL, result.LimitsMemory);
            Assert.Equal(2_000UL, result.LimitsCpu);
            Assert.Equal(0UL, result.RequestsMemory); // Should be 0 for missing variables
            Assert.Equal(0UL, result.RequestsCpu); // Should be 0 for missing variables
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

        try
        {
            // Act
            var result1 = KubernetesMetadata.FromEnvironmentVariables(Prefix1);
            var result2 = KubernetesMetadata.FromEnvironmentVariables(Prefix2);

            // Assert
            Assert.Equal(1_073_741_824UL, result1.LimitsMemory);
            Assert.Equal(1_000UL, result1.LimitsCpu);
            Assert.Equal(2_147_483_648UL, result2.LimitsMemory);
            Assert.Equal(2_000UL, result2.LimitsCpu);
        }
        finally
        {
            tempSetup.Dispose();
        }
    }
}

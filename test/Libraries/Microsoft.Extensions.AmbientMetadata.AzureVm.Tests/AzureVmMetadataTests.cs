// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataTests
{
    [Fact]
    public void ShouldConstructObject()
    {
        var instance = new AzureVmMetadata();
        Assert.NotNull(instance);
    }

    [Fact]
    public void ShouldHaveAllPropertiesNull()
    {
        // Arrange
        var obj = new AzureVmMetadata();
        object?[] properties = obj.GetType().GetProperties().Select(f => f.GetValue(obj)).ToArray();

        properties.Should().OnlyContain(x => x == null);
    }

    [Fact]
    public void LocationProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata
        {
            // Act
            Location = "West US 2"
        };

        // Assert
        metadata.Location.Should().Be("West US 2");
    }

    [Fact]
    public void NameProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { Name = "MyVM" };

        // Assert
        metadata.Name.Should().Be("MyVM");
    }

    [Fact]
    public void OfferProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { Offer = "WindowsServer" };

        // Assert
        metadata.Offer.Should().Be("WindowsServer");
    }

    [Fact]
    public void OsTypeProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { OsType = "Windows" };

        // Assert
        metadata.OsType.Should().Be("Windows");
    }

    [Fact]
    public void PlatformFaultDomainProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { PlatformFaultDomain = "0" };

        // Assert
        metadata.PlatformFaultDomain.Should().Be("0");
    }

    [Fact]
    public void PlatformUpdateDomainProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { PlatformUpdateDomain = "0" };

        // Assert
        metadata.PlatformUpdateDomain.Should().Be("0");
    }

    [Fact]
    public void PublisherProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { Publisher = "Microsoft" };

        // Assert
        metadata.Publisher.Should().Be("Microsoft");
    }

    [Fact]
    public void SkuProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { Sku = "Standard_D2_v3" };

        // Assert
        metadata.Sku.Should().Be("Standard_D2_v3");
    }

    [Fact]
    public void SubscriptionIdProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { SubscriptionId = "01234567-89ab-cdef-0123-456789abcdef" };

        // Assert
        metadata.SubscriptionId.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
    }

    [Fact]
    public void VersionProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata
        {
            // Act
            Version = "1.0.0"
        };

        // Assert
        metadata.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void VmIdProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { VmId = "01234567-89ab-cdef-0123-456789abcdef" };

        // Assert
        metadata.VmId.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
    }

    [Fact]
    public void VmScaleSetNameProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { VmScaleSetName = "scale set name" };

        // Assert
        metadata.VmScaleSetName.Should().Be("scale set name");
    }

    [Fact]
    public void VmSizeProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new AzureVmMetadata { VmSize = "1234567" };

        // Assert
        metadata.VmSize.Should().Be("1234567");
    }
}

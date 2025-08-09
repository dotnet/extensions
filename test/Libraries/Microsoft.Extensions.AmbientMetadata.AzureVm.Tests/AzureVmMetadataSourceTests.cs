// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.AmbientMetadata.Internal;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataSourceTests
{
    [Fact]
    public void Constructor_SetsSectionNameProperty_WhenArgumentsAreValid()
    {
        // Arrange
        IAzureVmMetadataProvider metadataProvider = Mock.Of<IAzureVmMetadataProvider>();
        string sectionName = "AzureMetadata";

        // Act
        var source = new AzureVmMetadataSource(metadataProvider, sectionName);

        // Assert
        Assert.Equal(sectionName, source.SectionName);
    }

    [Fact]
    public void Build_CallsGetMetadataAsyncOnMetadataProvider()
    {
        // Arrange
        Mock<IAzureVmMetadataProvider> metadataProvider = new Mock<IAzureVmMetadataProvider>();
        var metadata = new AzureVmMetadata();
        metadataProvider.Setup(m => m.GetMetadataAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        var source = new AzureVmMetadataSource(metadataProvider.Object, "Azure");

        // Act
        IConfigurationProvider provider = source.Build(Mock.Of<IConfigurationBuilder>());

        // Assert
        metadataProvider.Verify(m => m.GetMetadataAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Build_ConfiguresProviderWithValues()
    {
        // Arrange
        const string TestSectionName = "Azure";
        IAzureVmMetadataProvider metadataProvider = Mock.Of<IAzureVmMetadataProvider>();
        var metadata = new AzureVmMetadata
        {
            Location = "East US",
            Name = "TestVM",
            Offer = "Windows",
            OsType = "Windows",
            PlatformFaultDomain = "0",
            PlatformUpdateDomain = "1",
            Publisher = "Microsoft",
            Sku = "2019-Datacenter",
            SubscriptionId = "12345678-1234-1234-1234-123456789012",
            Version = "1.0.0",
            VmId = "12345678-1234-1234-1234-123456789012",
            VmScaleSetName = "TestScaleSet",
            VmSize = "Standard_D2_v2"
        };
        Mock.Get(metadataProvider).Setup(m => m.GetMetadataAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        var source = new AzureVmMetadataSource(metadataProvider, TestSectionName);

        // Act
        IConfigurationRoot config = new ConfigurationBuilder().Add(source).Build();

        // Assert
        Assert.Equal(metadata.Location, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Location)}"]);
        Assert.Equal(metadata.Name, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Name)}"]);
        Assert.Equal(metadata.Offer, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Offer)}"]);
        Assert.Equal(metadata.OsType, config[$"{TestSectionName}:{nameof(AzureVmMetadata.OsType)}"]);
        Assert.Equal(metadata.PlatformFaultDomain, config[$"{TestSectionName}:{nameof(AzureVmMetadata.PlatformFaultDomain)}"]);
        Assert.Equal(metadata.PlatformUpdateDomain, config[$"{TestSectionName}:{nameof(AzureVmMetadata.PlatformUpdateDomain)}"]);
        Assert.Equal(metadata.Publisher, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Publisher)}"]);
        Assert.Equal(metadata.Sku, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Sku)}"]);
        Assert.Equal(metadata.SubscriptionId, config[$"{TestSectionName}:{nameof(AzureVmMetadata.SubscriptionId)}"]);
        Assert.Equal(metadata.Version, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Version)}"]);
        Assert.Equal(metadata.VmId, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmId)}"]);
        Assert.Equal(metadata.VmScaleSetName, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmScaleSetName)}"]);
        Assert.Equal(metadata.VmSize, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmSize)}"]);
    }

    [Fact]
    public void Build_ConfiguresProviderWithEmptyValues_WhenMetadataIsEmpty()
    {
        // Arrange
        const string TestSectionName = "Azure";
        IAzureVmMetadataProvider metadataProvider = Mock.Of<IAzureVmMetadataProvider>();
        var metadata = new AzureVmMetadata();
        Mock.Get(metadataProvider).Setup(m => m.GetMetadataAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        var source = new AzureVmMetadataSource(metadataProvider, TestSectionName);

        // Act
        IConfigurationRoot config = new ConfigurationBuilder().Add(source).Build();

        // Assert
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Name)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Offer)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.OsType)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.PlatformFaultDomain)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.PlatformUpdateDomain)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Publisher)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Sku)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.SubscriptionId)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.Version)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmId)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmScaleSetName)}"]);
        Assert.Equal(string.Empty, config[$"{TestSectionName}:{nameof(AzureVmMetadata.VmSize)}"]);
    }
}

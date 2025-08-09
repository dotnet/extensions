// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenAnyNullArgument_ShouldThrowArgumentNullException()
    {
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        serviceCollection.Invoking(x => ((IServiceCollection)null!).AddAzureVmMetadata(config.GetSection(string.Empty)))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => x.AddAzureVmMetadata((Action<AzureVmMetadata>)null!))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => ((IServiceCollection)null!).AddAzureVmMetadata(_ => { }))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => x.AddAzureVmMetadata((IConfigurationSection)null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenConfigurationSection_ShouldRegisterMetadataFromIt()
    {
        // Arrange
        var testData = new AzureVmMetadata
        {
            Location = "eastus",
            Name = "my-vm",
            Offer = "WindowsServer",
            OsType = "Windows",
            PlatformFaultDomain = "0",
            PlatformUpdateDomain = "0",
            Publisher = "MicrosoftWindowsServer",
            Sku = "2019-Datacenter",
            SubscriptionId = "12345678-1234-5678-abcd-1234567890ab",
            Version = "latest",
            VmId = "abcdefgh-1234-5678-abcd-1234567890ab",
            VmScaleSetName = "my-vm-scale-set",
            VmSize = "Standard_DS2_v2"
        };

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Location)}"] = testData.Location,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Name)}"] = testData.Name,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Offer)}"] = testData.Offer,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.OsType)}"] = testData.OsType,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.PlatformFaultDomain)}"] = testData.PlatformFaultDomain,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.PlatformUpdateDomain)}"] = testData.PlatformUpdateDomain,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Publisher)}"] = testData.Publisher,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Sku)}"] = testData.Sku,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.SubscriptionId)}"] = testData.SubscriptionId,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.Version)}"] = testData.Version,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.VmId)}"] = testData.VmId,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.VmScaleSetName)}"] = testData.VmScaleSetName,
                [$"{nameof(AzureVmMetadata)}:{nameof(AzureVmMetadata.VmSize)}"] = testData.VmSize,
            })
            .Build();

        IConfigurationSection configurationSection = config
            .GetSection(nameof(AzureVmMetadata));

        // Act
        using ServiceProvider provider = new ServiceCollection()
            .AddAzureVmMetadata(configurationSection)
            .BuildServiceProvider();
        AzureVmMetadata metadata = provider
            .GetRequiredService<IOptions<AzureVmMetadata>>().Value;

        // Assert
        metadata.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void GivenActionDelegate_ShouldRegisterMetadataFromIt()
    {
        // Arrange
        var testData = new AzureVmMetadata
        {
            Location = "eastus",
            Name = "my-vm",
            Offer = "WindowsServer",
            OsType = "Windows",
            PlatformFaultDomain = "0",
            PlatformUpdateDomain = "0",
            Publisher = "MicrosoftWindowsServer",
            Sku = "2019-Datacenter",
            SubscriptionId = "12345678-1234-5678-abcd-1234567890ab",
            Version = "latest",
            VmId = "abcdefgh-1234-5678-abcd-1234567890ab",
            VmScaleSetName = "my-vm-scale-set",
            VmSize = "Standard_DS2_v2"
        };

        // Act
        using ServiceProvider provider = new ServiceCollection()
            .AddAzureVmMetadata(m =>
            {
                m.Location = testData.Location;
                m.Name = testData.Name;
                m.Offer = testData.Offer;
                m.OsType = testData.OsType;
                m.PlatformFaultDomain = testData.PlatformFaultDomain;
                m.PlatformUpdateDomain = testData.PlatformUpdateDomain;
                m.Publisher = testData.Publisher;
                m.Sku = testData.Sku;
                m.SubscriptionId = testData.SubscriptionId;
                m.Version = testData.Version;
                m.VmId = testData.VmId;
                m.VmScaleSetName = testData.VmScaleSetName;
                m.VmSize = testData.VmSize;
            })
            .BuildServiceProvider();
        AzureVmMetadata metadata = provider
            .GetRequiredService<IOptions<AzureVmMetadata>>().Value;

        // Assert
        metadata.Should().BeEquivalentTo(testData);
    }
}

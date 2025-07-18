// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataHostBuilderExtensionsTests
{
    [Theory]
    [InlineData("ambientmetadata:azurevm")]
    [InlineData("customSection:ambientmetadata:azurevm")]
    public void GivenMetadata_RegistersOptions_HostBuilder(string? sectionName)
    {
        var metadata = new AzureVmMetadata
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

        var config = new Dictionary<string, string?>
        {
            [$"{sectionName}:{nameof(AzureVmMetadata.Location)}"] = metadata.Location,
            [$"{sectionName}:{nameof(AzureVmMetadata.Name)}"] = metadata.Name,
            [$"{sectionName}:{nameof(AzureVmMetadata.Offer)}"] = metadata.Offer,
            [$"{sectionName}:{nameof(AzureVmMetadata.OsType)}"] = metadata.OsType,
            [$"{sectionName}:{nameof(AzureVmMetadata.PlatformFaultDomain)}"] = metadata.PlatformFaultDomain,
            [$"{sectionName}:{nameof(AzureVmMetadata.PlatformUpdateDomain)}"] = metadata.PlatformUpdateDomain,
            [$"{sectionName}:{nameof(AzureVmMetadata.Publisher)}"] = metadata.Publisher,
            [$"{sectionName}:{nameof(AzureVmMetadata.Sku)}"] = metadata.Sku,
            [$"{sectionName}:{nameof(AzureVmMetadata.SubscriptionId)}"] = metadata.SubscriptionId,
            [$"{sectionName}:{nameof(AzureVmMetadata.Version)}"] = metadata.Version,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmId)}"] = metadata.VmId,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmScaleSetName)}"] = metadata.VmScaleSetName,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmSize)}"] = metadata.VmSize,
        };

        IHostBuilder hostBuilder = FakeHost.CreateBuilder();
        if (sectionName is not null)
        {
            hostBuilder.UseAzureVmMetadata(sectionName);
        }
        else
        {
            hostBuilder.UseAzureVmMetadata();
        }

        using IHost host = hostBuilder
            .ConfigureHostConfiguration(configBuilder => configBuilder.AddInMemoryCollection(config))
            .Build();

        host.Services.GetRequiredService<IOptions<AzureVmMetadata>>().Value.Should().BeEquivalentTo(metadata);
    }

    [Theory]
    [InlineData("ambientmetadata:azurevm")]
    [InlineData("customSection:ambientmetadata:azurevm")]
    public void GivenMetadata_RegistersOptions_HostApplicationBuilder(string? sectionName)
    {
        var metadata = new AzureVmMetadata
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

        var config = new Dictionary<string, string?>
        {
            [$"{sectionName}:{nameof(AzureVmMetadata.Location)}"] = metadata.Location,
            [$"{sectionName}:{nameof(AzureVmMetadata.Name)}"] = metadata.Name,
            [$"{sectionName}:{nameof(AzureVmMetadata.Offer)}"] = metadata.Offer,
            [$"{sectionName}:{nameof(AzureVmMetadata.OsType)}"] = metadata.OsType,
            [$"{sectionName}:{nameof(AzureVmMetadata.PlatformFaultDomain)}"] = metadata.PlatformFaultDomain,
            [$"{sectionName}:{nameof(AzureVmMetadata.PlatformUpdateDomain)}"] = metadata.PlatformUpdateDomain,
            [$"{sectionName}:{nameof(AzureVmMetadata.Publisher)}"] = metadata.Publisher,
            [$"{sectionName}:{nameof(AzureVmMetadata.Sku)}"] = metadata.Sku,
            [$"{sectionName}:{nameof(AzureVmMetadata.SubscriptionId)}"] = metadata.SubscriptionId,
            [$"{sectionName}:{nameof(AzureVmMetadata.Version)}"] = metadata.Version,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmId)}"] = metadata.VmId,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmScaleSetName)}"] = metadata.VmScaleSetName,
            [$"{sectionName}:{nameof(AzureVmMetadata.VmSize)}"] = metadata.VmSize,
        };

        HostApplicationBuilder hostBuilder = Host.CreateEmptyApplicationBuilder(new());
        if (sectionName is not null)
        {
            hostBuilder.UseAzureVmMetadata(sectionName);
        }
        else
        {
            hostBuilder.UseAzureVmMetadata();
        }

        hostBuilder.Configuration.AddInMemoryCollection(config);
        using IHost host = hostBuilder.Build();

        host.Services.GetRequiredService<IOptions<AzureVmMetadata>>().Value.Should().BeEquivalentTo(metadata);
    }
}

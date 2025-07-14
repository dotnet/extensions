// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.AmbientMetadata.Internal;

/// <summary>
/// Provides virtual configuration source for information about Azure Virtual Machine instances.
/// </summary>
internal sealed class AzureVmMetadataSource : IConfigurationSource
{
    private readonly IAzureVmMetadataProvider _metadataProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureVmMetadataSource"/> class.
    /// </summary>
    /// <param name="metadataProvider">Azure VM metadata provider.</param>
    /// <param name="sectionName">Section name in configuration to save configuration values to.</param>
    public AzureVmMetadataSource(IAzureVmMetadataProvider metadataProvider, string sectionName)
    {
        _metadataProvider = metadataProvider;
        SectionName = sectionName;
    }

    /// <summary>
    /// Gets configuration section name.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Builds <see cref="IConfigurationProvider"/> from <see cref="IAzureVmMetadataProvider"/>.
    /// </summary>
    /// <param name="builder">Used to build the application configuration.</param>
    /// <returns>The configuration provider.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        AzureVmMetadata metadata = _metadataProvider.GetMetadataAsync(CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource())
        {
            { $"{SectionName}:location", metadata.Location ?? string.Empty },
            { $"{SectionName}:name", metadata.Name ?? string.Empty },
            { $"{SectionName}:offer", metadata.Offer ?? string.Empty },
            { $"{SectionName}:ostype", metadata.OsType ?? string.Empty },
            { $"{SectionName}:platformfaultdomain", metadata.PlatformFaultDomain ?? string.Empty },
            { $"{SectionName}:platformupdatedomain", metadata.PlatformUpdateDomain ?? string.Empty },
            { $"{SectionName}:publisher", metadata.Publisher ?? string.Empty },
            { $"{SectionName}:sku", metadata.Sku ?? string.Empty },
            { $"{SectionName}:subscriptionid", metadata.SubscriptionId ?? string.Empty },
            { $"{SectionName}:version", metadata.Version ?? string.Empty },
            { $"{SectionName}:vmid", metadata.VmId ?? string.Empty },
            { $"{SectionName}:vmscalesetname", metadata.VmScaleSetName ?? string.Empty },
            { $"{SectionName}:vmsize", metadata.VmSize ?? string.Empty }
        };

        return provider;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Metadata for applications running on Azure Virtual Machines.
/// Represents the Azure VM metadata supplied by the Azure Instance Metadata service:
/// <see href="https://learn.microsoft.com/azure/virtual-machines/instance-metadata-service"/>.
/// </summary>
/// <remarks>
/// This class is initialized from an HTTP response received from a network endpoint.
/// Therefore, in case of a network failure, it can happen that all properties will have empty values.
/// </remarks>
public class AzureVmMetadata
{
    /// <summary>
    /// Gets or sets the Azure Region the VM is running in.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the name of the VM.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the offer information for the VM image.
    /// </summary>
    public string? Offer { get; set; }

    /// <summary>
    /// Gets or sets the type of the OS (Linux or Windows).
    /// </summary>
    public string? OsType { get; set; }

    /// <summary>
    /// Gets or sets the fault domain the VM is running in.
    /// </summary>
    public string? PlatformFaultDomain { get; set; }

    /// <summary>
    /// Gets or sets the update domain the VM is running in.
    /// </summary>
    public string? PlatformUpdateDomain { get; set; }

    /// <summary>
    /// Gets or sets the Publisher of the VM image.
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the specific SKU for the VM image.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the subscription id in which the VM is executing.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the version of the VM image.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the VM.
    /// </summary>
    public string? VmId { get; set; }

    /// <summary>
    /// Gets or sets the name of the scale set in which the VM is executing.
    /// </summary>
    public string? VmScaleSetName { get; set; }

    /// <summary>
    /// Gets or sets the VM size.
    /// </summary>
    public string? VmSize { get; set; }
}

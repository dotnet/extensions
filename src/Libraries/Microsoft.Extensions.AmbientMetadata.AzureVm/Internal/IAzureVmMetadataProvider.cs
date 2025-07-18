// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AmbientMetadata.Internal;

/// <summary>
/// An interface for obtaining metadata of the Azure Virtual Machine instances.
/// </summary>
internal interface IAzureVmMetadataProvider
{
    /// <summary>
    /// Retrieves the metadata of the Azure Virtual Machine instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An instance of <see cref="AzureVmMetadata"/>.</returns>
    /// <remarks>For more information, see <see href="https://learn.microsoft.com/azure/virtual-machines/instance-metadata-service">here.</see>.</remarks>
    Task<AzureVmMetadata> GetMetadataAsync(CancellationToken cancellationToken);
}

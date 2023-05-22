// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// An interface for utilization publisher.
/// </summary>
public interface IResourceUtilizationPublisher
{
    /// <summary>
    /// This method is called to update subscribers when new utilization state has been computed.
    /// </summary>
    /// <param name="utilization">The utilization struct to be published.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the publish operation.</param>
    /// <returns>ValueTask because operation can finish synchronously.</returns>
    ValueTask PublishAsync(Utilization utilization, CancellationToken cancellationToken);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Defines the contract for a resource utilization publisher that gets invoked whenever resource utilization is computed.
/// </summary>
[Obsolete("This interface is obsolete. Instead, use observable instruments from Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceUtilizationInstruments.")]
public interface IResourceUtilizationPublisher
{
    /// <summary>
    /// This method is invoked by the monitoring infrastructure whenever resource utilization is computed.
    /// </summary>
    /// <param name="utilization">A snapshot of the system's current resource utilization.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the publish operation.</param>
    /// <returns>A value task to track the publication operation.</returns>
    ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken);
}

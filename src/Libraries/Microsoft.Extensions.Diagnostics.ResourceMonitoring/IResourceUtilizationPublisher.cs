// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Defines the contract for a resource utilization publisher that gets invoked whenever resource utilization is computed.
/// </summary>
#if !NET5_0_OR_GREATER
#pragma warning disable CS0436 // Type conflicts with imported type
#endif
[Obsolete(DiagnosticIds.Obsoletions.IResourceUtilizationPublisherMessage,
    DiagnosticId = DiagnosticIds.Obsoletions.IResourceUtilizationPublisherDiagId,
    UrlFormat = DiagnosticIds.UrlFormat)]
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

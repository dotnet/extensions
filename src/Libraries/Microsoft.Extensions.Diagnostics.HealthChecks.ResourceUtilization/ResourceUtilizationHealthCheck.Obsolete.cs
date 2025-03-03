// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a health check for in-container resources <see cref="IHealthCheck"/>.
/// </summary>
internal sealed partial class ResourceUtilizationHealthCheck : IHealthCheck
{
#pragma warning disable CS0436 // Type conflicts with imported type
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
            DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
            UrlFormat = DiagnosticIds.UrlFormat)]
    public void ObsoleteConstructor(IResourceMonitor dataTracker) => _dataTracker = Throw.IfNull(dataTracker);

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
#pragma warning disable IDE0060 // Remove unused parameter
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    public Task<HealthCheckResult> ObsoleteCheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var utilization = _dataTracker!.GetUtilization(_options.SamplingWindow);
        return ResourceUtilizationHealthCheck.EvaluateHealthStatusAsync(utilization.CpuUsedPercentage, utilization.MemoryUsedPercentage, _options);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a health check for in-container resources <see cref="IHealthCheck"/>.
/// </summary>
internal sealed class ResourceUtilizationHealthCheck : IHealthCheck
{
    private static readonly Task<HealthCheckResult> _healthy = Task.FromResult(HealthCheckResult.Healthy());
    private readonly ResourceUtilizationHealthCheckOptions _options;
    private readonly IResourceUtilizationTracker _dataTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="dataTracker">The datatracker.</param>
    public ResourceUtilizationHealthCheck(IOptions<ResourceUtilizationHealthCheckOptions> options,
        IResourceUtilizationTracker dataTracker)
    {
        _options = Throw.IfMemberNull(options, options.Value);
        _dataTracker = Throw.IfNull(dataTracker);
    }

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var utilization = _dataTracker.GetUtilization(_options.SamplingWindow);
        if (utilization.CpuUsedPercentage > _options.CpuThresholds?.UnhealthyUtilizationPercentage)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("CPU usage is above the limit"));
        }

        if (utilization.MemoryUsedPercentage > _options.MemoryThresholds?.UnhealthyUtilizationPercentage)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory usage is above the limit"));
        }

        if (utilization.CpuUsedPercentage > _options.CpuThresholds?.DegradedUtilizationPercentage)
        {
            return Task.FromResult(HealthCheckResult.Degraded("CPU usage is close to the limit"));
        }

        if (utilization.MemoryUsedPercentage > _options.MemoryThresholds?.DegradedUtilizationPercentage)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Memory usage is close to the limit"));
        }

        return _healthy;
    }
}

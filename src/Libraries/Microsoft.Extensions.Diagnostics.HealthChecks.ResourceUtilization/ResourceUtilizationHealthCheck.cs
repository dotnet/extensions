// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
    private readonly ResourceUtilizationHealthCheckOptions _options;
    private readonly IResourceMonitor _dataTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="dataTracker">The datatracker.</param>
    public ResourceUtilizationHealthCheck(IOptions<ResourceUtilizationHealthCheckOptions> options,
        IResourceMonitor dataTracker)
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
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            { nameof(utilization.CpuUsedPercentage), utilization.CpuUsedPercentage },
            { nameof(utilization.MemoryUsedPercentage), utilization.MemoryUsedPercentage },
        };

        bool cpuUnhealthy = utilization.CpuUsedPercentage > _options.CpuThresholds.UnhealthyUtilizationPercentage;
        bool memoryUnhealthy = utilization.MemoryUsedPercentage > _options.MemoryThresholds.UnhealthyUtilizationPercentage;

        if (cpuUnhealthy || memoryUnhealthy)
        {
            string message = string.Empty;
            if (cpuUnhealthy && memoryUnhealthy)
            {
                message = "CPU and Memory";
            }
            else if (cpuUnhealthy)
            {
                message = "CPU";
            }
            else
            {
                message = "Memory";
            }

            message += " usage is above the limit";
            return Task.FromResult(HealthCheckResult.Unhealthy(message, default, data));
        }

        bool cpuDegraded = utilization.CpuUsedPercentage > _options.CpuThresholds.DegradedUtilizationPercentage;
        bool memoryDegraded = utilization.MemoryUsedPercentage > _options.MemoryThresholds.DegradedUtilizationPercentage;

        if (cpuDegraded || memoryDegraded)
        {
            string message = string.Empty;
            if (cpuDegraded && memoryDegraded)
            {
                message = "CPU and Memory";
            }
            else if (cpuDegraded)
            {
                message = "CPU";
            }
            else
            {
                message = "Memory";
            }

            message += " usage is close to the limit";
            return Task.FromResult(HealthCheckResult.Degraded(message, default, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(default, data));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Performs metering and logging telemetry on the HealthReport.
/// </summary>
internal sealed class TelemetryHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly HealthCheckMetrics _metrics;
    private readonly ILogger _logger;
    private readonly bool _logOnlyUnhealthy;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryHealthCheckPublisher"/> class.
    /// </summary>
    /// <param name="metrics">The metrics.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Creation options.</param>
    public TelemetryHealthCheckPublisher(HealthCheckMetrics metrics, ILogger<TelemetryHealthCheckPublisher> logger, IOptions<TelemetryHealthCheckPublisherOptions> options)
    {
        var value = Throw.IfMemberNull(options, options.Value);
        _logOnlyUnhealthy = Throw.IfMemberNull(options, options.Value.LogOnlyUnhealthy);
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Performs logging and metering before publishing the provided report.
    /// </summary>
    /// <param name="report">The <see cref="HealthReport"/>. The result of executing a set of health checks.</param>
    /// <param name="cancellationToken">Not used in the current implementation.</param>
    /// <returns>Task.CompletedTask.</returns>
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(report);

        if (report.Status == HealthStatus.Healthy)
        {
            if (!_logOnlyUnhealthy)
            {
                Log.Healthy(_logger, report.Status);
            }
        }
        else
        {
            var stringBuilder = PoolFactory.SharedStringBuilderPool.Get();

            // Construct string showing list of all health entries status and description for logs
            string separator = string.Empty;
            foreach (var entry in report.Entries)
            {
                if (entry.Value.Status != HealthStatus.Healthy)
                {
                    _metrics.UnhealthyHealthCheckCounter.RecordMetric(entry.Key, entry.Value.Status);
                }

                _ = stringBuilder.Append(separator)
                    .Append(entry.Key)
                    .Append(": {")
                    .Append("status: ")
                    .Append(entry.Value.Status.ToInvariantString())
                    .Append(", description: ")
                    .Append(entry.Value.Description)
                    .Append('}');

                separator = ", ";
            }

            Log.Unhealthy(_logger, report.Status, stringBuilder);
            PoolFactory.SharedStringBuilderPool.Return(stringBuilder);
        }

        _metrics.HealthCheckReportCounter.RecordMetric(report.Status);

        return Task.CompletedTask;
    }
}

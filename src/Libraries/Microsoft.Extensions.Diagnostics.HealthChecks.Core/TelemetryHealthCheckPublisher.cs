// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Performs metering and logging telemetry on the HealthReport.
/// </summary>
internal sealed class TelemetryHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly HealthCheckReportCounter _healthCheckReportCounter;
    private readonly UnhealthyHealthCheckCounter _unhealthyHealthCheckCounter;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryHealthCheckPublisher"/> class.
    /// </summary>
    /// <param name="meter">The meter.</param>
    /// <param name="logger">The logger.</param>
    public TelemetryHealthCheckPublisher(Meter<TelemetryHealthCheckPublisher> meter, ILogger<TelemetryHealthCheckPublisher> logger)
    {
        _logger = logger;
        _healthCheckReportCounter = Metric.CreateHealthCheckReportCounter(meter);
        _unhealthyHealthCheckCounter = Metric.CreateUnhealthyHealthCheckCounter(meter);
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
            Log.Healthy(_logger, report.Status);
            _healthCheckReportCounter.RecordMetric(true, report.Status);
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
                    _unhealthyHealthCheckCounter.RecordMetric(entry.Key, entry.Value.Status);
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

            _healthCheckReportCounter.RecordMetric(false, report.Status);
        }

        return Task.CompletedTask;
    }
}

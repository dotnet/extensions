// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a health check for network utilization <see cref="IHealthCheck"/>.
/// </summary>
internal sealed class NetworkUtilizationHealthCheck : IHealthCheck, IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly NetworkUtilizationHealthCheckOptions _options;
    private long _establishedConnections;
    private long _synReceivedConnections;

#pragma warning disable EA0014 // The async method doesn't support cancellation
    public static Task<HealthCheckResult> EvaluateHealthStatusAsync(long establishedConnections, long synReceivedConnections, NetworkUtilizationHealthCheckOptions options)
    {
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            { "EstablishedConnections", establishedConnections },
            { "SynReceivedConnections", synReceivedConnections },
        };

        bool establishedUnhealthy = establishedConnections > options.EstablishedConnectionsThresholds.UnhealthyCount;
        bool synReceivedUnhealthy = synReceivedConnections > options.SynReceivedConnectionsThresholds.UnhealthyCount;

        if (establishedUnhealthy || synReceivedUnhealthy)
        {
            string message;
            if (establishedUnhealthy && synReceivedUnhealthy)
            {
                message = "Network connections count is above the limit";
            }
            else if (establishedUnhealthy)
            {
                message = "Established connections count is above the limit";
            }
            else
            {
                message = "SYN received connections count is above the limit";
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(message, data: data));
        }

        bool establishedDegraded = establishedConnections > options.EstablishedConnectionsThresholds.DegradedCount;
        bool synReceivedDegraded = synReceivedConnections > options.SynReceivedConnectionsThresholds.DegradedCount;

        if (establishedDegraded || synReceivedDegraded)
        {
            string message;
            if (establishedDegraded && synReceivedDegraded)
            {
                message = "Network connections count is close to the limit";
            }
            else if (establishedDegraded)
            {
                message = "Established connections count is close to the limit";
            }
            else
            {
                message = "SYN received connections count is close to the limit";
            }

            return Task.FromResult(HealthCheckResult.Degraded(message, data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(data: data));
    }
#pragma warning restore EA0014 // The async method doesn't support cancellation

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public NetworkUtilizationHealthCheck(IOptions<NetworkUtilizationHealthCheckOptions> options)
    {
        _options = Throw.IfMemberNull(options, options.Value);

        _meterListener = new()
        {
            InstrumentPublished = OnInstrumentPublished
        };

        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();
    }

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // The connection counts are reported per network type (IPv4 and IPv6),
        // so reset the totals before recording and sum the measurements.
        _establishedConnections = 0;
        _synReceivedConnections = 0;
        _meterListener.RecordObservableInstruments();

        return EvaluateHealthStatusAsync(_establishedConnections, _synReceivedConnections, _options);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _meterListener.Dispose();
        }
    }

    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        if (instrument.Meter.Name == ResourceUtilizationInstruments.MeterName
            && instrument.Name == ResourceUtilizationInstruments.SystemNetworkConnections)
        {
            listener.EnableMeasurementEvents(instrument);
        }
    }

    private void OnMeasurementRecorded(
        Instrument instrument, long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        string? networkState = null;
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (tag.Key == "system.network.state")
            {
                networkState = tag.Value as string;
                break;
            }
        }

        switch (networkState)
        {
            case "established":
                _establishedConnections += measurement;
                break;
            case "syn_recv":
                _synReceivedConnections += measurement;
                break;
        }
    }
}

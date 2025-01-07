// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a health check for in-container resources <see cref="IHealthCheck"/>.
/// </summary>
internal sealed class ResourceUtilizationHealthCheck : IHealthCheck, IDisposable
{
    private readonly double _multiplier;
    private readonly MeterListener? _meterListener;
    private readonly ResourceUtilizationHealthCheckOptions _options;
    private readonly IResourceMonitor _dataTracker;
    private double _cpuUsedPercentage;
    private double _memoryUsedPercentage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="dataTracker">The datatracker.</param>
    public ResourceUtilizationHealthCheck(IOptions<ResourceUtilizationHealthCheckOptions> options, IResourceMonitor dataTracker)
    {
#if NETFRAMEWORK
        _multiplier = 1;
#else
        // Due to a bug on Windows https://github.com/dotnet/extensions/issues/5472,
        // the CPU utilization comes in the range [0, 100].
        if (OperatingSystem.IsWindows())
        {
            _multiplier = 1;
        }

        // On Linux, the CPU utilization comes in the correct range [0, 1], which we will be converting to percentage.
        else
        {
#pragma warning disable S109 // Magic numbers should not be used
            _multiplier = 100;
#pragma warning restore S109 // Magic numbers should not be used
        }
#endif
        _options = Throw.IfMemberNull(options, options.Value);
        _dataTracker = Throw.IfNull(dataTracker);

        if (_options.UseObservableResourceMonitoringInstruments)
        {
            _meterListener = new()
            {
                InstrumentPublished = OnInstrumentPublished
            };

            _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
            _meterListener.Start();
        }
    }

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.UseObservableResourceMonitoringInstruments)
        {
            _meterListener!.RecordObservableInstruments();
        }
        else
        {
            var utilization = _dataTracker.GetUtilization(_options.SamplingWindow);
            _cpuUsedPercentage = utilization.CpuUsedPercentage;
            _memoryUsedPercentage = utilization.MemoryUsedPercentage;
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            { "CpuUsedPercentage", _cpuUsedPercentage },
            { "MemoryUsedPercentage", _memoryUsedPercentage },
        };

        bool cpuUnhealthy = _cpuUsedPercentage > _options.CpuThresholds.UnhealthyUtilizationPercentage;
        bool memoryUnhealthy = _memoryUsedPercentage > _options.MemoryThresholds.UnhealthyUtilizationPercentage;

        if (cpuUnhealthy || memoryUnhealthy)
        {
            string message;
            if (cpuUnhealthy && memoryUnhealthy)
            {
                message = "CPU and memory usage is above the limit";
            }
            else if (cpuUnhealthy)
            {
                message = "CPU usage is above the limit";
            }
            else
            {
                message = "Memory usage is above the limit";
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(message, default, data));
        }

        bool cpuDegraded = _cpuUsedPercentage > _options.CpuThresholds.DegradedUtilizationPercentage;
        bool memoryDegraded = _memoryUsedPercentage > _options.MemoryThresholds.DegradedUtilizationPercentage;

        if (cpuDegraded || memoryDegraded)
        {
            string message;
            if (cpuDegraded && memoryDegraded)
            {
                message = "CPU and memory usage is close to the limit";
            }
            else if (cpuDegraded)
            {
                message = "CPU usage is close to the limit";
            }
            else
            {
                message = "Memory usage is close to the limit";
            }

            return Task.FromResult(HealthCheckResult.Degraded(message, default, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(default, data));
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
            _meterListener?.Dispose();
        }
    }

    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        if (instrument.Meter.Name is "Microsoft.Extensions.Diagnostics.ResourceMonitoring")
        {
            listener.EnableMeasurementEvents(instrument, this);
        }
    }

    private void OnMeasurementRecorded(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        if (state != this)
        {
            return;
        }

        switch (instrument.Name)
        {
            case "process.cpu.utilization":
            case "container.cpu.limit.utilization":
                _cpuUsedPercentage = measurement * _multiplier;
                break;
            case "dotnet.process.memory.virtual.utilization":
            case "container.memory.limit.utilization":
                _memoryUsedPercentage = measurement * _multiplier;
                break;
        }
    }
}

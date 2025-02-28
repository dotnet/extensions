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
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a health check for in-container resources <see cref="IHealthCheck"/>.
/// </summary>
internal sealed partial class ResourceUtilizationHealthCheck : IHealthCheck, IDisposable
{
#if !NETFRAMEWORK
    private const double Hundred = 100.0;
#endif
    private readonly double _multiplier;
    private readonly MeterListener? _meterListener;
    private readonly ResourceUtilizationHealthCheckOptions _options;
    private IResourceMonitor? _dataTracker;
    private double _cpuUsedPercentage;
    private double _memoryUsedPercentage;

#pragma warning disable EA0014 // The async method doesn't support cancellation
    public static Task<HealthCheckResult> EvaluateHealthStatusAsync(double cpuUsedPercentage, double memoryUsedPercentage, ResourceUtilizationHealthCheckOptions options)
    {
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            { "CpuUsedPercentage", cpuUsedPercentage },
            { "MemoryUsedPercentage", memoryUsedPercentage },
        };

        bool cpuUnhealthy = cpuUsedPercentage > options.CpuThresholds.UnhealthyUtilizationPercentage;
        bool memoryUnhealthy = memoryUsedPercentage > options.MemoryThresholds.UnhealthyUtilizationPercentage;

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

        bool cpuDegraded = cpuUsedPercentage > options.CpuThresholds.DegradedUtilizationPercentage;
        bool memoryDegraded = memoryUsedPercentage > options.MemoryThresholds.DegradedUtilizationPercentage;

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
#pragma warning restore EA0014 // The async method doesn't support cancellation

#if !NETFRAMEWORK
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="dataTracker">The datatracker.</param>
    /// <param name="rmOptions">The Resource Monitoring options.</param>
    public ResourceUtilizationHealthCheck(IOptions<ResourceUtilizationHealthCheckOptions> options, IResourceMonitor dataTracker, IOptions<ResourceMonitoringOptions> rmOptions)
    {
        _options = Throw.IfMemberNull(options, options.Value);
        if (!_options.UseObservableResourceMonitoringInstruments)
        {
            ObsoleteConstructor(dataTracker);
            return;
        }

        // On Windows there was a bug https://github.com/dotnet/extensions/issues/5472,
        // so the CPU utilization might come in the range [0, 100], which means we don't need to multiply it by 100.
        var rmOptionsValue = Throw.IfMemberNull(rmOptions, rmOptions.Value);
        if (OperatingSystem.IsWindows() && !rmOptionsValue.UseZeroToOneRangeForMetrics)
        {
            _multiplier = 1;
        }

        // On Linux, the CPU utilization always comes in the correct range [0, 1], which we need to convert to percentage.
        else
        {
            _multiplier = Hundred;
        }

        _meterListener = new()
        {
            InstrumentPublished = OnInstrumentPublished
        };

        _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _meterListener.Start();
    }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUtilizationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="dataTracker">The datatracker.</param>
    public ResourceUtilizationHealthCheck(IOptions<ResourceUtilizationHealthCheckOptions> options, IResourceMonitor dataTracker)
    {
        _options = Throw.IfMemberNull(options, options.Value);
        if (!_options.UseObservableResourceMonitoringInstruments)
        {
            ObsoleteConstructor(dataTracker);
            return;
        }

        _multiplier = 1;
        _meterListener = new()
        {
            InstrumentPublished = OnInstrumentPublished
        };

        _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _meterListener.Start();
    }
#endif

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.UseObservableResourceMonitoringInstruments)
        {
            return ObsoleteCheckHealthAsync(cancellationToken);
        }

        _meterListener!.RecordObservableInstruments();

        return EvaluateHealthStatusAsync(_cpuUsedPercentage, _memoryUsedPercentage, _options);
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
        if (instrument.Meter.Name == ResourceUtilizationInstruments.MeterName)
        {
            listener.EnableMeasurementEvents(instrument);
        }
    }

    private void OnMeasurementRecorded(
        Instrument instrument, double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        switch (instrument.Name)
        {
            case ResourceUtilizationInstruments.ProcessCpuUtilization:
            case ResourceUtilizationInstruments.ContainerCpuLimitUtilization:
                _cpuUsedPercentage = measurement * _multiplier;
                break;
            case ResourceUtilizationInstruments.ProcessMemoryUtilization:
            case ResourceUtilizationInstruments.ContainerMemoryLimitUtilization:
                _memoryUsedPercentage = measurement * _multiplier;
                break;
        }
    }
}

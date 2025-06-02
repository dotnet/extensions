// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDiskMetrics
{
    private const string DeviceKey = "system.device";
    private const string DirectionKey = "disk.io.direction";

    private static readonly KeyValuePair<string, object?> _directionReadTag = new(DirectionKey, "read");
    private static readonly KeyValuePair<string, object?> _directionWriteTag = new(DirectionKey, "write");
    private readonly ILogger<WindowsDiskMetrics> _logger;
    private readonly Dictionary<string, WindowsDiskIoRatePerfCounter> _diskIoRateCounters = new();
    private WindowsDiskIoTimePerfCounter? _diskIoTimePerfCounter;

    public WindowsDiskMetrics(
        ILogger<WindowsDiskMetrics>? logger,
        IMeterFactory meterFactory,
        IPerformanceCounterFactory performanceCounterFactory,
        TimeProvider timeProvider,
        IOptions<ResourceMonitoringOptions> options)
    {
        _logger = logger ?? NullLogger<WindowsDiskMetrics>.Instance;
        if (!options.Value.EnableSystemDiskIoMetrics)
        {
            return;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // It's a false-positive, see: https://github.com/dotnet/roslyn-analyzers/issues/6912.
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        Meter meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        InitializeDiskCounters(performanceCounterFactory, timeProvider);

        // The metric is aligned with
        // https://opentelemetry.io/docs/specs/semconv/system/system-metrics/#metric-systemdiskio
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskIo,
            GetDiskIoMeasurements,
            unit: "By",
            description: "Disk bytes transferred");

        // The metric is aligned with
        // https://opentelemetry.io/docs/specs/semconv/system/system-metrics/#metric-systemdiskoperations
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskOperations,
            GetDiskOperationMeasurements,
            unit: "{operation}",
            description: "Disk operations");

        // The metric is aligned with
        // https://opentelemetry.io/docs/specs/semconv/system/system-metrics/#metric-systemdiskio_time
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskIoTime,
            GetDiskIoTimeMeasurements,
            unit: "s",
            description: "Time disk spent activated");
    }

#pragma warning disable CA1031 // Do not catch general exception types
    private void InitializeDiskCounters(IPerformanceCounterFactory performanceCounterFactory, TimeProvider timeProvider)
    {
        const string DiskCategoryName = "LogicalDisk";
        string[] instanceNames = performanceCounterFactory.GetCategoryInstances(DiskCategoryName)
            .Where(instanceName => !instanceName.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (instanceNames.Length == 0)
        {
            return;
        }

        // Initialize disk performance counters for "system.disk.io_time" metric
        try
        {
            var ioTimePerfCounter = new WindowsDiskIoTimePerfCounter(
                performanceCounterFactory,
                timeProvider,
                DiskCategoryName,
                WindowsDiskPerfCounterNames.DiskIdleTimeCounter,
                instanceNames);
            ioTimePerfCounter.InitializeDiskCounters();
            _diskIoTimePerfCounter = ioTimePerfCounter;
        }
        catch (Exception ex)
        {
            Log.DiskIoPerfCounterException(_logger, WindowsDiskPerfCounterNames.DiskIdleTimeCounter, ex.Message);
        }

        // Initialize disk performance counters for "system.disk.io" and "system.disk.operations" metrics
        List<string> diskIoRatePerfCounterNames =
        [
            WindowsDiskPerfCounterNames.DiskWriteBytesCounter,
            WindowsDiskPerfCounterNames.DiskReadBytesCounter,
            WindowsDiskPerfCounterNames.DiskWritesCounter,
            WindowsDiskPerfCounterNames.DiskReadsCounter,
        ];
        foreach (string counterName in diskIoRatePerfCounterNames)
        {
            try
            {
                var ratePerfCounter = new WindowsDiskIoRatePerfCounter(
                    performanceCounterFactory,
                    timeProvider,
                    DiskCategoryName,
                    counterName,
                    instanceNames);
                ratePerfCounter.InitializeDiskCounters();
                _diskIoRateCounters.Add(counterName, ratePerfCounter);
            }
            catch (Exception ex)
            {
                Log.DiskIoPerfCounterException(_logger, counterName, ex.Message);
            }
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types

    private IEnumerable<Measurement<long>> GetDiskIoMeasurements()
    {
        List<Measurement<long>> measurements = [];

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskWriteBytesCounter, out WindowsDiskIoRatePerfCounter? perSecondWriteCounter))
        {
            perSecondWriteCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in perSecondWriteCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionWriteTag, new(DeviceKey, pair.Key) }));
            }
        }

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskReadBytesCounter, out WindowsDiskIoRatePerfCounter? perSecondReadCounter))
        {
            perSecondReadCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in perSecondReadCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionReadTag, new(DeviceKey, pair.Key) }));
            }
        }

        return measurements;
    }

    private IEnumerable<Measurement<long>> GetDiskOperationMeasurements()
    {
        List<Measurement<long>> measurements = [];

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskWritesCounter, out WindowsDiskIoRatePerfCounter? writeCounter))
        {
            writeCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in writeCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionWriteTag, new(DeviceKey, pair.Key) }));
            }
        }

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskReadsCounter, out WindowsDiskIoRatePerfCounter? readCounter))
        {
            readCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in readCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionReadTag, new(DeviceKey, pair.Key) }));
            }
        }

        return measurements;
    }

    private IEnumerable<Measurement<double>> GetDiskIoTimeMeasurements()
    {
        List<Measurement<double>> measurements = [];
        if (_diskIoTimePerfCounter != null)
        {
            _diskIoTimePerfCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, double> pair in _diskIoTimePerfCounter.TotalSeconds)
            {
                measurements.Add(new Measurement<double>(pair.Value, new TagList { new(DeviceKey, pair.Key) }));
            }
        }

        return measurements;
    }
}

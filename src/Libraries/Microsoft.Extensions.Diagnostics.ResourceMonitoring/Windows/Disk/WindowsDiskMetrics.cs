// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.Versioning;
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
    private readonly Dictionary<string, WindowsDiskIoRatePerfCounters> _diskIoRateCounters = new();

    public WindowsDiskMetrics(
        IMeterFactory meterFactory,
        IPerformanceCounterFactory performanceCounterFactory,
        TimeProvider timeProvider,
        IOptions<ResourceMonitoringOptions> options)
    {
        if (!options.Value.EnableDiskIoMetrics)
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
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/system/system-metrics.md#metric-systemdiskio
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskIo,
            GetDiskIoMeasurements,
            unit: "By",
            description: "Disk bytes transferred");

        // The metric is aligned with
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/system/system-metrics.md#metric-systemdiskoperations
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskOperations,
            GetDiskOperationMeasurements,
            unit: "{operation}",
            description: "Disk operations");
    }

    private void InitializeDiskCounters(IPerformanceCounterFactory performanceCounterFactory, TimeProvider timeProvider)
    {
        const string DiskCategoryName = "LogicalDisk";
        string[] instanceNames = performanceCounterFactory.GetCategoryInstances(DiskCategoryName);
        if (instanceNames.Length == 0)
        {
            return;
        }

        List<string> diskIoRatePerformanceCounters =
        [
            WindowsDiskPerfCounterNames.DiskWriteBytesCounter,
            WindowsDiskPerfCounterNames.DiskReadBytesCounter,
            WindowsDiskPerfCounterNames.DiskWritesCounter,
            WindowsDiskPerfCounterNames.DiskReadsCounter,
        ];
        foreach (string counterName in diskIoRatePerformanceCounters)
        {
            try
            {
                var ratePerfCounter = new WindowsDiskIoRatePerfCounters(
                    performanceCounterFactory,
                    timeProvider,
                    DiskCategoryName,
                    counterName,
                    instanceNames);
                ratePerfCounter.InitializeDiskCounters();
                _diskIoRateCounters.Add(counterName, ratePerfCounter);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Debug.WriteLine("Error initializing disk performance counter: " + ex.Message);
            }
        }
    }

    private IEnumerable<Measurement<long>> GetDiskIoMeasurements()
    {
        List<Measurement<long>> measurements = [];

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskWriteBytesCounter, out WindowsDiskIoRatePerfCounters? perSecondWriteCounter))
        {
            perSecondWriteCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in perSecondWriteCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionWriteTag, new(DeviceKey, pair.Key) }));
            }
        }

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskReadBytesCounter, out WindowsDiskIoRatePerfCounters? perSecondReadCounter))
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

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskWritesCounter, out WindowsDiskIoRatePerfCounters? writeCounter))
        {
            writeCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in writeCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionWriteTag, new(DeviceKey, pair.Key) }));
            }
        }

        if (_diskIoRateCounters.TryGetValue(WindowsDiskPerfCounterNames.DiskReadsCounter, out WindowsDiskIoRatePerfCounters? readCounter))
        {
            readCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in readCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionReadTag, new(DeviceKey, pair.Key) }));
            }
        }

        return measurements;
    }
}

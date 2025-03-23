// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

#if NETCOREAPP
using System.Runtime.Versioning;
#endif

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

#if NETCOREAPP
[SupportedOSPlatform("windows")]
#endif
internal sealed class WindowsDiskMetrics
{
    private const string LogicalDiskCategory = "LogicalDisk";

    private const string DeviceKey = "system.device";

    private const string DiskWriteBytesCounter = "Disk Write Bytes/sec";

    private const string DiskReadBytesCounter = "Disk Read Bytes/sec";

    private static readonly KeyValuePair<string, object?> _directionReadTag = new("disk.io.direction", "read");

    private static readonly KeyValuePair<string, object?> _directionWriteTag = new("disk.io.direction", "write");

    private readonly Dictionary<string, WindowsDiskPerfCounters> _perfCounters = new();

    public WindowsDiskMetrics(IMeterFactory meterFactory, IOptions<ResourceMonitoringOptions> options)
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

        InitializeDiskCounters();

        // The metric is aligned with
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/system/system-metrics.md#metric-systemdiskio
        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskIo,
            GetDiskIoMeasurements,
            unit: "By",
            description: "Disk bytes transferred");
    }

    private void InitializeDiskCounters()
    {
        var diskCategory = new PerformanceCounterCategory(LogicalDiskCategory);

        foreach (string counterName in _performanceCounters)
        {
            try
            {
                var diskPerfCounter = new WindowsDiskPerfCounters(diskCategory, counterName);
                diskPerfCounter.InitializeDiskCounters();
                _perfCounters.Add(counterName, diskPerfCounter);
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

        if (_perfCounters.TryGetValue(DiskWriteBytesCounter, out WindowsDiskPerfCounters? diskWriteBytesPerfCounter))
        {
            diskWriteBytesPerfCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in diskWriteBytesPerfCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionWriteTag, new(DeviceKey, pair.Key) }));
            }
        }

        if (_perfCounters.TryGetValue(DiskReadBytesCounter, out WindowsDiskPerfCounters? diskReadBytesPerfCounter))
        {
            diskReadBytesPerfCounter.UpdateDiskCounters();
            foreach (KeyValuePair<string, long> pair in diskReadBytesPerfCounter.TotalCountDict)
            {
                measurements.Add(new Measurement<long>(pair.Value, new TagList { _directionReadTag, new(DeviceKey, pair.Key) }));
            }
        }

        return measurements;
    }

    private static readonly List<string> _performanceCounters =
    [
        DiskWriteBytesCounter,
        DiskReadBytesCounter,
    ];
}

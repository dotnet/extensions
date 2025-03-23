// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

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
        if (!options.Value.EnabledDiskIoMetrics)
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

        _ = meter.CreateObservableCounter(
            ResourceUtilizationInstruments.SystemDiskIo,
            GetDiskIoGetMeasurements,
            unit: "By",
            description: "Disk bytes transferred");
    }

    private void InitializeDiskCounters()
    {
        var diskCategory = new PerformanceCounterCategory(LogicalDiskCategory);
        string[]? instanceNames = diskCategory.GetInstanceNames();

        foreach (string counterName in _performanceCounters)
        {
            try
            {
                var diskPerfCounter = new WindowsDiskPerfCounters(diskCategory, counterName);
                diskPerfCounter.InitializeDiskCounters();
                _perfCounters.Add(counterName, diskPerfCounter);
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                // ignored
            }
        }
    }

    private IEnumerable<Measurement<long>> GetDiskIoGetMeasurements()
    {
        WindowsDiskPerfCounters diskWriteBytesPerfCounter = _perfCounters[DiskWriteBytesCounter];
        WindowsDiskPerfCounters diskReadBytesPerfCounter = _perfCounters[DiskReadBytesCounter];

        diskWriteBytesPerfCounter.UpdateDiskCounters();
        diskReadBytesPerfCounter.UpdateDiskCounters();

        List<Measurement<long>> measurements = [];
        foreach (KeyValuePair<string, long> diskWriteBytes in diskWriteBytesPerfCounter.TotalCountDict)
        {
            measurements.Add(new Measurement<long>(diskWriteBytes.Value, new TagList { _directionWriteTag, new(DeviceKey, diskWriteBytes.Key) }));
        }

        foreach (KeyValuePair<string, long> diskReadBytes in diskReadBytesPerfCounter.TotalCountDict)
        {
            measurements.Add(new Measurement<long>(diskReadBytes.Value, new TagList { _directionReadTag, new(DeviceKey, diskReadBytes.Key) }));
        }

        return measurements;
    }

    private static readonly List<string> _performanceCounters =
    [
        DiskWriteBytesCounter,
        DiskReadBytesCounter,
    ];
}

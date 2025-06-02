// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk;

internal sealed class LinuxSystemDiskMetrics
{
    // The kernel's block layer always reports counts in 512-byte "sectors" regardless of the underlying device's real block size
    // https://docs.kernel.org/block/stat.html#read-sectors-write-sectors-discard-sectors
    private const int LinuxDiskSectorSize = 512;
    private const int MinimumDiskStatsRefreshIntervalInSeconds = 10;
    private const string DeviceKey = "system.device";
    private const string DirectionKey = "disk.io.direction";

    private static readonly KeyValuePair<string, object?> _directionReadTag = new(DirectionKey, "read");
    private static readonly KeyValuePair<string, object?> _directionWriteTag = new(DirectionKey, "write");
    private readonly ILogger<LinuxSystemDiskMetrics> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IDiskStatsReader _diskStatsReader;
    private readonly object _lock = new();
    private readonly Dictionary<string, DiskStats> _baselineDiskStatsDict = [];
    private List<DiskStats> _diskStatsSnapshot = [];
    private DateTimeOffset _lastRefreshTime = DateTimeOffset.MinValue;

    public LinuxSystemDiskMetrics(
        ILogger<LinuxSystemDiskMetrics>? logger,
        IMeterFactory meterFactory,
        IOptions<ResourceMonitoringOptions> options,
        TimeProvider timeProvider,
        IDiskStatsReader diskStatsReader)
    {
        _logger = logger ?? NullLogger<LinuxSystemDiskMetrics>.Instance;
        _timeProvider = timeProvider;
        _diskStatsReader = diskStatsReader;
        if (!options.Value.EnableSystemDiskIoMetrics)
        {
            return;
        }

        // We need to read the disk stats once to get the baseline values
        _baselineDiskStatsDict = GetAllDiskStats().ToDictionary(d => d.DeviceName);

#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // It's a false-positive, see: https://github.com/dotnet/roslyn-analyzers/issues/6912.
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        Meter meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

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

    private IEnumerable<Measurement<long>> GetDiskIoMeasurements()
    {
        List<Measurement<long>> measurements = [];
        List<DiskStats> diskStatsSnapshot = GetDiskStatsSnapshot();

        foreach (DiskStats diskStats in diskStatsSnapshot)
        {
            _ = _baselineDiskStatsDict.TryGetValue(diskStats.DeviceName, out DiskStats? baselineDiskStats);
            long readBytes = (long)(diskStats.SectorsRead - baselineDiskStats?.SectorsRead ?? 0L) * LinuxDiskSectorSize;
            long writeBytes = (long)(diskStats.SectorsWritten - baselineDiskStats?.SectorsWritten ?? 0L) * LinuxDiskSectorSize;
            measurements.Add(new Measurement<long>(readBytes, new TagList { _directionReadTag, new(DeviceKey, diskStats.DeviceName) }));
            measurements.Add(new Measurement<long>(writeBytes, new TagList { _directionWriteTag, new(DeviceKey, diskStats.DeviceName) }));
        }

        return measurements;
    }

    private IEnumerable<Measurement<long>> GetDiskOperationMeasurements()
    {
        List<Measurement<long>> measurements = [];
        List<DiskStats> diskStatsSnapshot = GetDiskStatsSnapshot();

        foreach (DiskStats diskStats in diskStatsSnapshot)
        {
            _ = _baselineDiskStatsDict.TryGetValue(diskStats.DeviceName, out DiskStats? baselineDiskStats);
            long readCount = (long)(diskStats.ReadsCompleted - baselineDiskStats?.ReadsCompleted ?? 0L);
            long writeCount = (long)(diskStats.WritesCompleted - baselineDiskStats?.WritesCompleted ?? 0L);
            measurements.Add(new Measurement<long>(readCount, new TagList { _directionReadTag, new(DeviceKey, diskStats.DeviceName) }));
            measurements.Add(new Measurement<long>(writeCount, new TagList { _directionWriteTag, new(DeviceKey, diskStats.DeviceName) }));
        }

        return measurements;
    }

    private IEnumerable<Measurement<double>> GetDiskIoTimeMeasurements()
    {
        List<Measurement<double>> measurements = [];
        List<DiskStats> diskStatsSnapshot = GetDiskStatsSnapshot();

        foreach (DiskStats diskStats in diskStatsSnapshot)
        {
            _ = _baselineDiskStatsDict.TryGetValue(diskStats.DeviceName, out DiskStats? baselineDiskStats);
            double ioTimeSeconds = (diskStats.TimeIoMs - baselineDiskStats?.TimeIoMs ?? 0) / 1000.0; // Convert to seconds
            measurements.Add(new Measurement<double>(ioTimeSeconds, new TagList { new(DeviceKey, diskStats.DeviceName) }));
        }

        return measurements;
    }

    private List<DiskStats> GetDiskStatsSnapshot()
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_diskStatsSnapshot.Count == 0 || (now - _lastRefreshTime).TotalSeconds > MinimumDiskStatsRefreshIntervalInSeconds)
            {
                _diskStatsSnapshot = GetAllDiskStats();
                _lastRefreshTime = now;
            }
        }

        return _diskStatsSnapshot;
    }

    private List<DiskStats> GetAllDiskStats()
    {
        try
        {
            List<DiskStats> diskStatsList = _diskStatsReader.ReadAll();

            // We should not include ram, loop, or dm(device-mapper) devices in the disk stats, should we?
            diskStatsList = diskStatsList
                .Where(d => !d.DeviceName.StartsWith("ram", StringComparison.OrdinalIgnoreCase)
                            && !d.DeviceName.StartsWith("loop", StringComparison.OrdinalIgnoreCase)
                            && !d.DeviceName.StartsWith("dm-", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return diskStatsList;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.HandleDiskStatsException(_logger, ex.Message);
        }

        return [];
    }
}

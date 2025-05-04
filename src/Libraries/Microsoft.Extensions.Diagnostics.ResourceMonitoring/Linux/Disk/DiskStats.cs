// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk;

/// <summary>
/// Represents one line of statistics from "/proc/diskstats"
/// See https://www.kernel.org/doc/Documentation/ABI/testing/procfs-diskstats
/// </summary>
public class DiskStats
{
    public int MajorNumber { get; set; }
    public int MinorNumber { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public ulong ReadsCompleted { get; set; }
    public ulong ReadsMerged { get; set; }
    public ulong SectorsRead { get; set; }
    public ulong TimeReadingMs { get; set; }
    public ulong WritesCompleted { get; set; }
    public ulong WritesMerged { get; set; }
    public ulong SectorsWritten { get; set; }
    public ulong TimeWritingMs { get; set; }
    public ulong IoInProgress { get; set; }
    public ulong TimeIoMs { get; set; }
    public ulong WeightedTimeIoMs { get; set; }

    // The following fields are available starting from kernel 4.18; if absent, remain 0
    public ulong DiscardsCompleted { get; set; }
    public ulong DiscardsMerged { get; set; }
    public ulong SectorsDiscarded { get; set; }
    public ulong TimeDiscardingMs { get; set; }

    // The following fields are available starting from kernel 5.5; if absent, remain 0
    public ulong FlushRequestsCompleted { get; set; }
    public ulong TimeFlushingMs { get; set; }
}

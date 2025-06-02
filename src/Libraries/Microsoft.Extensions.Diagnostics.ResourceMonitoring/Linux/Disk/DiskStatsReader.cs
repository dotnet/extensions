// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk;

/// <summary>
/// Handles reading and parsing of Linux procfs-diskstats file(/proc/diskstats).
/// </summary>
internal sealed class DiskStatsReader(IFileSystem fileSystem) : IDiskStatsReader
{
    private static readonly FileInfo _diskStatsFile = new("/proc/diskstats");
    private static readonly ObjectPool<BufferWriter<char>> _sharedBufferWriterPool = BufferWriterPool.CreateBufferWriterPool<char>();

    /// <summary>
    /// Reads and returns all disk statistics entries.
    /// </summary>
    /// <returns>List of <see cref="DiskStats"/>.</returns>
    public List<DiskStats> ReadAll()
    {
        var diskStatsList = new List<DiskStats>();

        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        using IEnumerator<ReadOnlyMemory<char>> enumerableLines = fileSystem.ReadAllByLines(_diskStatsFile, bufferWriter.Buffer).GetEnumerator();

        while (enumerableLines.MoveNext())
        {
            string line = enumerableLines.Current.Trim().ToString();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                DiskStats stat = DiskStatsReader.ParseLine(line);
                diskStatsList.Add(stat);
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignore parsing errors
            }
        }

        return diskStatsList;
    }

    /// <summary>
    /// Parses one line of text into a DiskStats object.
    /// </summary>
    /// <param name="line">one line in "/proc/diskstats".</param>
    /// <returns>parsed DiskStats object.</returns>
    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "These numbers represent fixed field indices in the Linux /proc/diskstats format")]
    private static DiskStats ParseLine(string line)
    {
        // Split by any whitespace and remove empty entries
#pragma warning disable EA0009
        string[] parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
#pragma warning restore EA0009

        if (parts.Length < 14)
        {
            throw new FormatException($"Not enough fields: expected at least 14, got {parts.Length}");
        }

        // See https://www.kernel.org/doc/Documentation/ABI/testing/procfs-diskstats
        var diskStats = new DiskStats
        {
            MajorNumber = int.Parse(parts[0], CultureInfo.InvariantCulture),
            MinorNumber = int.Parse(parts[1], CultureInfo.InvariantCulture),
            DeviceName = parts[2],
            ReadsCompleted = ulong.Parse(parts[3], CultureInfo.InvariantCulture),
            ReadsMerged = ulong.Parse(parts[4], CultureInfo.InvariantCulture),
            SectorsRead = ulong.Parse(parts[5], CultureInfo.InvariantCulture),
            TimeReadingMs = uint.Parse(parts[6], CultureInfo.InvariantCulture),
            WritesCompleted = ulong.Parse(parts[7], CultureInfo.InvariantCulture),
            WritesMerged = ulong.Parse(parts[8], CultureInfo.InvariantCulture),
            SectorsWritten = ulong.Parse(parts[9], CultureInfo.InvariantCulture),
            TimeWritingMs = uint.Parse(parts[10], CultureInfo.InvariantCulture),
            IoInProgress = uint.Parse(parts[11], CultureInfo.InvariantCulture),
            TimeIoMs = uint.Parse(parts[12], CultureInfo.InvariantCulture),
            WeightedTimeIoMs = uint.Parse(parts[13], CultureInfo.InvariantCulture)
        };

        // Parse additional fields if present
        if (parts.Length >= 18)
        {
            diskStats.DiscardsCompleted = ulong.Parse(parts[14], CultureInfo.InvariantCulture);
            diskStats.DiscardsMerged = ulong.Parse(parts[15], CultureInfo.InvariantCulture);
            diskStats.SectorsDiscarded = ulong.Parse(parts[16], CultureInfo.InvariantCulture);
            diskStats.TimeDiscardingMs = uint.Parse(parts[17], CultureInfo.InvariantCulture);
        }

        if (parts.Length >= 20)
        {
            diskStats.FlushRequestsCompleted = ulong.Parse(parts[18], CultureInfo.InvariantCulture);
            diskStats.TimeFlushingMs = uint.Parse(parts[19], CultureInfo.InvariantCulture);
        }

        return diskStats;
    }
}

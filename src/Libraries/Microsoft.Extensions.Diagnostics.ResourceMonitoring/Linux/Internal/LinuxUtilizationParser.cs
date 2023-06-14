// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Parses Linux files to retrieve resource utilization data.
/// </summary>
internal sealed class LinuxUtilizationParser
{
    /// <remarks>
    /// File contains the amount of CPU time (in microseconds) available to the group during each accounting period.
    /// </remarks>
    private static readonly FileInfo _cpuCfsQuotaUs = new("/sys/fs/cgroup/cpu/cpu.cfs_quota_us");

    /// <remarks>
    /// File contains the length of the accounting period in microseconds.
    /// </remarks>
    private static readonly FileInfo _cpuCfsPeriodUs = new("/sys/fs/cgroup/cpu/cpu.cfs_period_us");

    /// <remarks>
    /// Stat file contains information about all CPUs and their time.
    /// </remarks>
    /// <remarks>
    /// The file has format of whitespace separated values. Each value has its own meaning and unit.
    /// To know which value we read, why and what it means refer to proc (5) man page (its POSIX).
    /// </remarks>
    private static readonly FileInfo _procStat = new("/proc/stat");

    /// <remarks>
    /// File that contains information about available memory.
    /// </remarks>
    private static readonly FileInfo _memInfo = new("/proc/meminfo");

    /// <remarks>
    /// List of available CPUs for host.
    /// </remarks>
    private static readonly FileInfo _cpuSetCpus = new("/sys/fs/cgroup/cpuset/cpuset.cpus");

    /// <remarks>
    /// Cgroup memory limit.
    /// </remarks>
    private static readonly FileInfo _memoryLimitInBytes = new("/sys/fs/cgroup/memory/memory.limit_in_bytes");

    /// <summary>
    /// Cgroup memory stats.
    /// </summary>
    /// <remarks>
    /// Single line representing used memory by cgroup in bytes.
    /// </remarks>
    private static readonly FileInfo _memoryUsageInBytes = new("/sys/fs/cgroup/memory/memory.usage_in_bytes");

    /// <summary>
    /// Cgroup memory stats.
    /// </summary>
    /// <remarks>
    /// This file contains the details about memory usage.
    /// The format is (type of memory spent) (value) (unit of measure).
    /// </remarks>
    private static readonly FileInfo _memoryStat = new("/sys/fs/cgroup/memory/memory.stat");

    /// <summary>
    /// File containing usage in nanoseconds.
    /// </summary>
    /// <remarks>
    /// This value refers to the container/cgroup utilization.
    /// The format is single line with one number value.
    /// </remarks>
    private static readonly FileInfo _cpuacctUsage = new("/sys/fs/cgroup/cpuacct/cpuacct.usage");

    private readonly IFileSystem _fileSystem;
    private readonly long _userHz;
    private readonly ObjectPool<BufferWriter<char>> _buffers;

    public LinuxUtilizationParser(IFileSystem fileSystem, IUserHz userHz)
    {
        _fileSystem = fileSystem;
        _userHz = userHz.Value;
        _buffers = BufferWriterPool.CreateBufferWriterPool<char>(maxCapacity: 64);
    }

    public long GetCgroupCpuUsageInNanoseconds()
    {
        var buffer = _buffers.Get();
        _fileSystem.ReadAll(_cpuacctUsage, buffer);

        var usage = buffer.WrittenSpan;

        _ = GetNextNumber(usage, out var nanoseconds);

        if (nanoseconds == -1)
        {
            Throw.InvalidOperationException($"Could not get cpu usage from '{_cpuacctUsage}'. Expected positive number, but got '{new string(usage)}'.");
        }

        _buffers.Return(buffer);

        return nanoseconds;
    }

    public long GetHostCpuUsageInNanoseconds()
    {
        const string StartingTokens = "cpu ";
        const int NumberOfColumnsRepresentingCpuUsage = 8;
        const int NanosecondsInSecond = 1_000_000_000;

        var buffer = _buffers.Get();
        _fileSystem.ReadFirstLine(_procStat, buffer);

        var stat = buffer.WrittenSpan;
        var total = 0L;

        if (!buffer.WrittenSpan.StartsWith(StartingTokens))
        {
            Throw.InvalidOperationException($"Expected proc/stat to start with '{StartingTokens}' but it was '{new string(buffer.WrittenSpan)}'.");
        }

        stat = stat.Slice(StartingTokens.Length, stat.Length - StartingTokens.Length);

        for (var i = 0; i < NumberOfColumnsRepresentingCpuUsage; i++)
        {
            var next = GetNextNumber(stat, out var number);

            if (number != -1)
            {
                total += number;
            }

            if (next == -1)
            {
                Throw.InvalidOperationException(
                    $"'{_procStat}' should contain whitespace separated values according to POSIX. We've failed trying to get {i}th value. File content: '{new string(stat)}'.");
            }

            stat = stat.Slice(next, stat.Length - next);
        }

        _buffers.Return(buffer);

        return (long)(total / (double)_userHz * NanosecondsInSecond);
    }

    /// <remarks>
    /// When CGroup limits are set, we can calculate number of cores based on the file settings.
    /// It should be 99% of the cases when app is hosted in the container environment.
    /// Otherwise, we assume that all host's CPUs are available, which we read from proc/stat file.
    /// </remarks>
    public float GetCgroupLimitedCpus()
    {
        if (TryGetCpuUnitsFromCgroups(_fileSystem, out var cpus))
        {
            return cpus;
        }

        return GetHostCpuCount();
    }

    public ulong GetAvailableMemoryInBytes()
    {
        const long UnsetCgroupMemoryLimit = 9_223_372_036_854_771_712;

        var buffer = _buffers.Get();
        _fileSystem.ReadAll(_memoryLimitInBytes, buffer);

        var memoryBuffer = buffer.WrittenSpan;
        _ = GetNextNumber(memoryBuffer, out var maybeMemory);

        if (maybeMemory == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_memoryLimitInBytes}' content. Expected to find available memory in bytes but got '{new string(memoryBuffer)}' instead.");
        }

        _buffers.Return(buffer);

        if (maybeMemory == UnsetCgroupMemoryLimit)
        {
            return GetHostAvailableMemory();
        }

        return (ulong)maybeMemory;
    }

    public ulong GetMemoryUsageInBytes()
    {
        const string TotalInactiveFile = "total_inactive_file";

        var buffer = _buffers.Get();
        _fileSystem.ReadAll(_memoryStat, buffer);
        var memoryFile = buffer.WrittenSpan;

        var index = memoryFile.IndexOf(TotalInactiveFile.AsSpan());

        if (index == -1)
        {
            Throw.InvalidOperationException($"Unable to find total_inactive_file from '{_memoryStat}'.");
        }

        var inactiveMemorySlice = memoryFile.Slice(index + TotalInactiveFile.Length, memoryFile.Length - index - TotalInactiveFile.Length);
        _ = GetNextNumber(inactiveMemorySlice, out var inactiveMemory);

        if (inactiveMemory == -1)
        {
            Throw.InvalidOperationException($"The value of total_inactive_file found in '{_memoryStat}' is not a positive number: '{new string(inactiveMemorySlice)}'.");
        }

        buffer.Reset();

        _fileSystem.ReadAll(_memoryUsageInBytes, buffer);

        var containerMemoryUsageFile = buffer.WrittenSpan;
        var next = GetNextNumber(containerMemoryUsageFile, out var containerMemoryUsage);

        // this file format doesn't expect to contain anything after the number.
        if (containerMemoryUsage == -1)
        {
            Throw.InvalidOperationException(
                $"We tried to read '{_memoryUsageInBytes}', and we expected to get a positive number but instead it was: '{new string(containerMemoryUsageFile)}'.");
        }

        _buffers.Return(buffer);

        var memoryUsage = containerMemoryUsage - inactiveMemory;

        if (memoryUsage < 0)
        {
            Throw.InvalidOperationException($"The total memory usage read from '{_memoryUsageInBytes}' is lesser than inactive memory usage read from '{_memoryStat}'.");
        }

        return (ulong)memoryUsage;
    }

    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Shifting bits left by number n is multiplying the value by 2 to the power of n.")]
    public ulong GetHostAvailableMemory()
    {
        // The value we are interested in starts with this. We just want to make sure it is true.
        const string MemTotal = "MemTotal:";

        var buffer = _buffers.Get();
        _fileSystem.ReadFirstLine(_memInfo, buffer);
        var firstLine = buffer.WrittenSpan;

        if (!firstLine.StartsWith(MemTotal))
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected first line of the file to start with '{MemTotal}' but it was '{new string(firstLine)}' instead.");
        }

        var totalMemory = firstLine.Slice(MemTotal.Length, firstLine.Length - MemTotal.Length);

        var next = GetNextNumber(totalMemory, out var totalMemoryAvailable);

        if (totalMemoryAvailable == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected to get total memory usage on first line but we've got: '{new string(firstLine)}'.");
        }

        if (next == -1 || totalMemory.Length - next < 2)
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected to get memory usage followed by the unit (kB, MB, GB) but found no unit: '{new string(firstLine)}'.");
        }

        var unit = totalMemory.Slice(totalMemory.Length - 2, 2);
        var memory = (ulong)totalMemoryAvailable;

        var u = unit switch
        {
            "kB" => memory << 10,
            "MB" => memory << 20,
            "GB" => memory << 30,
            "TB" => memory << 40,
            _ => throw new InvalidOperationException(
                $"We tried to convert total memory usage value from '{_memInfo}' to bytes, but we've got a unit that we don't recognize: '{new string(unit)}'.")
        };

        _buffers.Return(buffer);

        return u;
    }

    /// <remarks>
    /// The file format is the following:
    /// 0-18
    /// So, it is array indexed number of cpus.
    /// </remarks>
    public float GetHostCpuCount()
    {
        var buffer = _buffers.Get();
        _fileSystem.ReadFirstLine(_cpuSetCpus, buffer);
        var stats = buffer.WrittenSpan;

        var start = stats.IndexOf("-", StringComparison.Ordinal);

        if (stats.IsEmpty || start == -1 || start == 0)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuSetCpus}'. Expected integer based range separated by dash (like 0-8) but got '{new string(stats)}'.");
        }

        var first = stats.Slice(0, start);
        var second = stats.Slice(start + 1, stats.Length - start - 1);

        _ = GetNextNumber(first, out var startCpu);
        var next = GetNextNumber(second, out var endCpu);

        if (startCpu == -1 || endCpu == -1 || endCpu < startCpu || next != -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuSetCpus}'. Expected integer based range separated by dash (like 0-8) but got '{new string(stats)}'.");
        }

        _buffers.Return(buffer);

        return endCpu - startCpu + 1;
    }

    /// <remarks>
    /// The input must contain only number. If there is something more than whitespace before the number, it will return failure.
    /// </remarks>
    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "We are adding another digit, so we need to multiply by ten.")]
    private static int GetNextNumber(ReadOnlySpan<char> buffer, out long number)
    {
        var numberStart = 0;

        while (numberStart < buffer.Length && char.IsWhiteSpace(buffer[numberStart]))
        {
            numberStart++;
        }

        if (numberStart == buffer.Length || !char.IsDigit(buffer[numberStart]))
        {
            number = -1;
            return -1;
        }

        var numberEnd = numberStart;
        number = 0;

        while (numberEnd < buffer.Length && char.IsDigit(buffer[numberEnd]))
        {
            var current = buffer[numberEnd] - '0';
            number *= 10;
            number += current;
            numberEnd++;
        }

        return numberEnd < buffer.Length ? numberEnd : -1;
    }

    private bool TryGetCpuUnitsFromCgroups(IFileSystem fileSystem, out float cpuUnits)
    {
        var buffer = _buffers.Get();
        fileSystem.ReadFirstLine(_cpuCfsQuotaUs, buffer);

        var quotaBuffer = buffer.WrittenSpan;

        if (quotaBuffer.IsEmpty || (quotaBuffer.Length == 2 && quotaBuffer[0] == '-' && quotaBuffer[1] == '1'))
        {
            cpuUnits = -1;
            return false;
        }

        var nextQuota = GetNextNumber(quotaBuffer, out var quota);

        if (quota == -1 || nextQuota != -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsQuotaUs}'. Expected an integer but got: '{new string(quotaBuffer)}'.");
        }

        buffer.Reset();

        fileSystem.ReadFirstLine(_cpuCfsPeriodUs, buffer);
        var periodBuffer = buffer.WrittenSpan;

        if (periodBuffer.IsEmpty || (periodBuffer.Length == 2 && periodBuffer[0] == '-' && periodBuffer[1] == '1'))
        {
            cpuUnits = -1;
            return false;
        }

        var nextPeriod = GetNextNumber(periodBuffer, out var period);

        if (period == -1 || nextPeriod != -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsPeriodUs}'. Expected to get an integer but got: '{new string(periodBuffer)}'.");
        }

        _buffers.Return(buffer);

        cpuUnits = (float)quota / period;
        return true;
    }
}

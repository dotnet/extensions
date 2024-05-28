// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

/// <remarks>
/// Parses Linux cgroup v1 files to retrieve resource utilization data.
/// This class is not thread safe.
/// When the same instance is called by multiple threads it may return corrupted data.
/// </remarks>
internal sealed class LinuxUtilizationParserCgroupV1 : ILinuxUtilizationParser
{
    private const float CpuShares = 1024;
    private static readonly ObjectPool<BufferWriter<char>> _sharedBufferWriterPool = BufferWriterPool.CreateBufferWriterPool<char>();

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

    /// <summary>
    /// CPU weights, also known as shares in cgroup v1, is used for resource allocation.
    /// </summary>
    private static readonly FileInfo _cpuPodWeight = new("/sys/fs/cgroup/cpu/cpu.shares");

    private readonly IFileSystem _fileSystem;
    private readonly long _userHz;

    public LinuxUtilizationParserCgroupV1(IFileSystem fileSystem, IUserHz userHz)
    {
        _fileSystem = fileSystem;
        _userHz = userHz.Value;
    }

    public long GetCgroupCpuUsageInNanoseconds()
    {
        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        _fileSystem.ReadAll(_cpuacctUsage, bufferWriter.Buffer);

        ReadOnlySpan<char> usage = bufferWriter.Buffer.WrittenSpan;

        _ = GetNextNumber(usage, out long nanoseconds);

        if (nanoseconds == -1)
        {
            Throw.InvalidOperationException($"Could not get cpu usage from '{_cpuacctUsage}'. Expected positive number, but got '{new string(usage)}'.");
        }

        return nanoseconds;
    }

    public long GetHostCpuUsageInNanoseconds()
    {
        const string StartingTokens = "cpu ";
        const int NumberOfColumnsRepresentingCpuUsage = 8;
        const int NanosecondsInSecond = 1_000_000_000;

        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        _fileSystem.ReadFirstLine(_procStat, bufferWriter.Buffer);

        ReadOnlySpan<char> stat = bufferWriter.Buffer.WrittenSpan;
        long total = 0L;

        if (!bufferWriter.Buffer.WrittenSpan.StartsWith(StartingTokens))
        {
            Throw.InvalidOperationException($"Expected proc/stat to start with '{StartingTokens}' but it was '{new string(bufferWriter.Buffer.WrittenSpan)}'.");
        }

        stat = stat.Slice(StartingTokens.Length, stat.Length - StartingTokens.Length);

        for (int i = 0; i < NumberOfColumnsRepresentingCpuUsage; i++)
        {
            int next = GetNextNumber(stat, out long number);

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

        return (long)(total / (double)_userHz * NanosecondsInSecond);
    }

    /// <remarks>
    /// When CGroup limits are set, we can calculate number of cores based on the file settings.
    /// It should be 99% of the cases when app is hosted in the container environment.
    /// Otherwise, we assume that all host's CPUs are available, which we read from proc/stat file.
    /// </remarks>
    public float GetCgroupLimitedCpus()
    {
        if (TryGetCpuUnitsFromCgroups(_fileSystem, out float cpus))
        {
            return cpus;
        }

        return GetHostCpuCount();
    }

    public float GetCgroupRequestCpu()
    {
        if (TryGetCgroupRequestCpu(_fileSystem, out float cpuUnits))
        {
            return cpuUnits;
        }

        return GetHostCpuCount();
    }

    public ulong GetAvailableMemoryInBytes()
    {
        const long UnsetCgroupMemoryLimit = 9_223_372_036_854_771_712;
        long maybeMemory = UnsetCgroupMemoryLimit;

        // Constrain the scope of the buffer because GetHostAvailableMemory is allocating its own buffer.
        using (ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool))
        {
            _fileSystem.ReadAll(_memoryLimitInBytes, bufferWriter.Buffer);

            ReadOnlySpan<char> memoryBuffer = bufferWriter.Buffer.WrittenSpan;
            _ = GetNextNumber(memoryBuffer, out maybeMemory);

            if (maybeMemory == -1)
            {
                Throw.InvalidOperationException($"Could not parse '{_memoryLimitInBytes}' content. Expected to find available memory in bytes but got '{new string(memoryBuffer)}' instead.");
            }
        }

        return maybeMemory == UnsetCgroupMemoryLimit
            ? GetHostAvailableMemory()
            : (ulong)maybeMemory;
    }

    public ulong GetMemoryUsageInBytes()
    {
        const string TotalInactiveFile = "total_inactive_file";

        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        _fileSystem.ReadAll(_memoryStat, bufferWriter.Buffer);
        ReadOnlySpan<char> memoryFile = bufferWriter.Buffer.WrittenSpan;

        int index = memoryFile.IndexOf(TotalInactiveFile.AsSpan());

        if (index == -1)
        {
            Throw.InvalidOperationException($"Unable to find total_inactive_file from '{_memoryStat}'.");
        }

        ReadOnlySpan<char> inactiveMemorySlice = memoryFile.Slice(index + TotalInactiveFile.Length, memoryFile.Length - index - TotalInactiveFile.Length);
        _ = GetNextNumber(inactiveMemorySlice, out long inactiveMemory);

        if (inactiveMemory == -1)
        {
            Throw.InvalidOperationException($"The value of total_inactive_file found in '{_memoryStat}' is not a positive number: '{new string(inactiveMemorySlice)}'.");
        }

        bufferWriter.Buffer.Reset();

        _fileSystem.ReadAll(_memoryUsageInBytes, bufferWriter.Buffer);

        ReadOnlySpan<char> containerMemoryUsageFile = bufferWriter.Buffer.WrittenSpan;
        int next = GetNextNumber(containerMemoryUsageFile, out long containerMemoryUsage);

        // this file format doesn't expect to contain anything after the number.
        if (containerMemoryUsage == -1)
        {
            Throw.InvalidOperationException(
                $"We tried to read '{_memoryUsageInBytes}', and we expected to get a positive number but instead it was: '{new string(containerMemoryUsageFile)}'.");
        }

        bufferWriter.Buffer.Reset();

        long memoryUsage = containerMemoryUsage - inactiveMemory;

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

        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        _fileSystem.ReadFirstLine(_memInfo, bufferWriter.Buffer);
        ReadOnlySpan<char> firstLine = bufferWriter.Buffer.WrittenSpan;

        if (!firstLine.StartsWith(MemTotal))
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected first line of the file to start with '{MemTotal}' but it was '{new string(firstLine)}' instead.");
        }

        ReadOnlySpan<char> totalMemory = firstLine.Slice(MemTotal.Length, firstLine.Length - MemTotal.Length);

        int next = GetNextNumber(totalMemory, out long totalMemoryAvailable);

        if (totalMemoryAvailable == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected to get total memory usage on first line but we've got: '{new string(firstLine)}'.");
        }

        if (next == -1 || totalMemory.Length - next < 2)
        {
            Throw.InvalidOperationException($"Could not parse '{_memInfo}'. We expected to get memory usage followed by the unit (kB, MB, GB) but found no unit: '{new string(firstLine)}'.");
        }

        ReadOnlySpan<char> unit = totalMemory.Slice(totalMemory.Length - 2, 2);
        ulong memory = (ulong)totalMemoryAvailable;

        ulong u = unit switch
        {
            "kB" => memory << 10,
            "MB" => memory << 20,
            "GB" => memory << 30,
            "TB" => memory << 40,
            _ => throw new InvalidOperationException(
                $"We tried to convert total memory usage value from '{_memInfo}' to bytes, but we've got a unit that we don't recognize: '{new string(unit)}'.")
        };

        return u;
    }

    /// <remarks>
    /// Comma-separated list of integers, with dashes ("-") to represent ranges. For example "0-1,5", or "0", or "1,2,3".
    /// Each value represents the zero-based index of a CPU.
    /// </remarks>
    public float GetHostCpuCount()
    {
        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        _fileSystem.ReadFirstLine(_cpuSetCpus, bufferWriter.Buffer);
        ReadOnlySpan<char> stats = bufferWriter.Buffer.WrittenSpan;

        if (stats.IsEmpty)
        {
            ThrowException(stats);
        }

        long cpuCount = 0L;

        // Iterate over groups (comma-separated)
        while (true)
        {
            int groupIndex = stats.IndexOf(',');

            ReadOnlySpan<char> group = groupIndex == -1 ? stats : stats.Slice(0, groupIndex);

            int rangeIndex = group.IndexOf('-');

            if (rangeIndex == -1)
            {
                // Single number
                _ = GetNextNumber(group, out long singleCpu);

                if (singleCpu == -1)
                {
                    ThrowException(stats);
                }

                cpuCount += 1;
            }
            else
            {
                // Range
                ReadOnlySpan<char> first = group.Slice(0, rangeIndex);
                _ = GetNextNumber(first, out long startCpu);

                ReadOnlySpan<char> second = group.Slice(rangeIndex + 1);
                int next = GetNextNumber(second, out long endCpu);

                if (endCpu == -1 || startCpu == -1 || endCpu < startCpu || next != -1)
                {
                    ThrowException(stats);
                }

                cpuCount += endCpu - startCpu + 1;
            }

            if (groupIndex == -1)
            {
                break;
            }

            stats = stats.Slice(groupIndex + 1);
        }

        return cpuCount;

        static void ThrowException(ReadOnlySpan<char> content) =>
            Throw.InvalidOperationException(
                $"Could not parse '{_cpuSetCpus}'. Expected comma-separated list of integers, with dashes (\"-\") based ranges (\"0\", \"2-6,12\") but got '{new string(content)}'.");
    }

    /// <remarks>
    /// The input must contain only number. If there is something more than whitespace before the number, it will return failure (-1).
    /// </remarks>
    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "We are adding another digit, so we need to multiply by ten.")]
    private static int GetNextNumber(ReadOnlySpan<char> buffer, out long number)
    {
        int numberStart = 0;

        while (numberStart < buffer.Length && char.IsWhiteSpace(buffer[numberStart]))
        {
            numberStart++;
        }

        if (numberStart == buffer.Length || !char.IsDigit(buffer[numberStart]))
        {
            number = -1;
            return -1;
        }

        int numberEnd = numberStart;
        number = 0;

        while (numberEnd < buffer.Length && char.IsDigit(buffer[numberEnd]))
        {
            int current = buffer[numberEnd] - '0';
            number *= 10;
            number += current;
            numberEnd++;
        }

        return numberEnd < buffer.Length ? numberEnd : -1;
    }

    private static bool TryGetCpuUnitsFromCgroups(IFileSystem fileSystem, out float cpuUnits)
    {
        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        fileSystem.ReadFirstLine(_cpuCfsQuotaUs, bufferWriter.Buffer);

        ReadOnlySpan<char> quotaBuffer = bufferWriter.Buffer.WrittenSpan;

        if (quotaBuffer.IsEmpty || (quotaBuffer.Length == 2 && quotaBuffer[0] == '-' && quotaBuffer[1] == '1'))
        {
            cpuUnits = -1;
            return false;
        }

        int nextQuota = GetNextNumber(quotaBuffer, out long quota);

        if (quota == -1 || nextQuota != -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsQuotaUs}'. Expected an integer but got: '{new string(quotaBuffer)}'.");
        }

        bufferWriter.Buffer.Reset();

        fileSystem.ReadFirstLine(_cpuCfsPeriodUs, bufferWriter.Buffer);
        ReadOnlySpan<char> periodBuffer = bufferWriter.Buffer.WrittenSpan;

        if (periodBuffer.IsEmpty || (periodBuffer.Length == 2 && periodBuffer[0] == '-' && periodBuffer[1] == '1'))
        {
            cpuUnits = -1;
            return false;
        }

        int nextPeriod = GetNextNumber(periodBuffer, out long period);

        if (period == -1 || nextPeriod != -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsPeriodUs}'. Expected to get an integer but got: '{new string(periodBuffer)}'.");
        }

        cpuUnits = (float)quota / period;
        return true;
    }

    /// <summary>
    /// In cgroup v1 the CPU shares is used to determine the CPU allocation.
    /// in cgroup v2 the CPU weight is used to determine the CPU allocation.
    /// To calculete CPU request in cgroup v2 we need to read the CPU weight and convert it to CPU shares.
    /// But for cgroup v1 we can read the CPU shares directly from the file.
    /// 1024 equals 1 CPU core.
    /// In cgroup v1 on some systems the location of the CPU shares file is different.
    /// </summary>
    private static bool TryGetCgroupRequestCpu(IFileSystem fileSystem, out float cpuUnits)
    {
        if (!fileSystem.Exists(_cpuPodWeight))
        {
            cpuUnits = 0;
            return false;
        }

        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        fileSystem.ReadFirstLine(_cpuPodWeight, bufferWriter.Buffer);
        ReadOnlySpan<char> cpuPodWeightBuffer = bufferWriter.Buffer.WrittenSpan;
        _ = GetNextNumber(cpuPodWeightBuffer, out long cpuPodWeight);

        if (cpuPodWeightBuffer.IsEmpty || (cpuPodWeightBuffer.Length == 2 && cpuPodWeightBuffer[0] == '-' && cpuPodWeightBuffer[1] == '1'))
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuPodWeight}' content. Expected to find CPU weight but got '{new string(cpuPodWeightBuffer)}' instead.");
        }

        float result = cpuPodWeight / CpuShares;
        cpuUnits = result;
        return true;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

/// <remarks>
/// Parses Linux files to retrieve resource utilization data.
/// This class is not thread safe.
/// When the same instance is called by multiple threads it may return corrupted data.
/// </remarks>
internal sealed class LinuxUtilizationParserCgroupV2 : ILinuxUtilizationParser
{
    private const int Thousand = 1000;
    private const int CpuShares = 1024;

    /// <remarks>
    /// File contains the amount of CPU time (in microseconds) available to the group during each accounting period.
    /// and the length of the accounting period in microseconds.
    /// In Cgroup V1 : /sys/fs/cgroup/cpu/cpu.cfs_quota_us and /sys/fs/cgroup/cpu/cpu.cfs_period_us.
    /// </remarks>
    private static readonly FileInfo _cpuCfsQuaotaPeriodUs = new("/sys/fs/cgroup/cpu.max");

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
    /// In Cgroup v1 : /sys/fs/cgroup/cpuset/cpuset.cpus.
    /// </remarks>
    private static readonly FileInfo _cpuSetCpus = new("/sys/fs/cgroup/cpuset.cpus.effective");

    /// <remarks>
    /// Cgroup memory limit.
    /// </remarks>
    private static readonly FileInfo _memoryLimitInBytes = new("/sys/fs/cgroup/memory.max");

    /// <summary>
    /// Cgroup memory stats.
    /// </summary>
    /// <remarks>
    /// Single line representing used memory by cgroup in bytes.
    /// </remarks>
    private static readonly FileInfo _memoryUsageInBytes = new("/sys/fs/cgroup/memory.current");

    /// <summary>
    /// Cgroup memory stats.
    /// </summary>
    /// <remarks>
    /// This file contains the details about memory usage.
    /// The format is (type of memory spent) (value) (unit of measure).
    /// </remarks>
    private static readonly FileInfo _memoryStat = new("/sys/fs/cgroup/memory.stat");

    /// <summary>
    /// File containing usage in nanoseconds.
    /// </summary>
    /// <remarks>
    /// This value refers to the container/cgroup utilization.
    /// The format is single line with one number value.
    /// </remarks>
    private static readonly FileInfo _cpuacctUsage = new("/sys/fs/cgroup/cpu.stat");

    /// <summary>
    /// CPU weights, also known as shares in cgroup v1, is used for resource allocation.
    /// </summary>
    private static readonly FileInfo _cpuPodWeight = new("/sys/fs/cgroup/cpu.weight");

    private readonly IFileSystem _fileSystem;
    private readonly long _userHz;
    private readonly BufferWriter<char> _buffer = new();

    public LinuxUtilizationParserCgroupV2(IFileSystem fileSystem, IUserHz userHz)
    {
        _fileSystem = fileSystem;
        _userHz = userHz.Value;
    }

    public long GetCgroupCpuUsageInNanoseconds()
    {
        // The value we are interested in starts with this. We just want to make sure it is true.
        const string Usage_usec = "usage_usec";

        // If the file doesn't exist, we assume that the system is a Host and we read the CPU usage from /proc/stat.
        if (!_fileSystem.Exists(_cpuacctUsage))
        {
            return GetHostCpuUsageInNanoseconds();
        }

        _fileSystem.ReadAll(_cpuacctUsage, _buffer);
        var usage = _buffer.WrittenSpan;

        if (!usage.StartsWith(Usage_usec))
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuacctUsage}'. We expected first line of the file to start with '{Usage_usec}' but it was '{new string(usage)}' instead.");
        }

        var cpuUsage = usage.Slice(Usage_usec.Length, usage.Length - Usage_usec.Length);

        var next = GetNextNumber(cpuUsage, out var microseconds);

        if (microseconds == -1)
        {
            Throw.InvalidOperationException($"Could not get cpu usage from '{_cpuacctUsage}'. Expected positive number, but got '{new string(usage)}'.");
        }

        _buffer.Reset();

        // In cgroup v2, the Units are microseconds for usage_usec.
        // We multiply by 1000 to convert to nanoseconds to keep the common calculation logic.
        return microseconds * Thousand;
    }

    public long GetHostCpuUsageInNanoseconds()
    {
        const string StartingTokens = "cpu ";
        const int NumberOfColumnsRepresentingCpuUsage = 8;
        const int NanosecondsInSecond = 1_000_000_000;

        _fileSystem.ReadFirstLine(_procStat, _buffer);

        var stat = _buffer.WrittenSpan;
        var total = 0L;

        if (!_buffer.WrittenSpan.StartsWith(StartingTokens))
        {
            Throw.InvalidOperationException($"Expected proc/stat to start with '{StartingTokens}' but it was '{new string(_buffer.WrittenSpan)}'.");
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

        _buffer.Reset();

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

    /// <remarks>
    /// If we are able to read the CPU share, we calculate the CPU request based on the weight by dividing it by 1024.
    /// If we can't read the CPU weight, we assume that the pod/vm cpu request is 1 core by default.
    /// </remarks>
    public float GetCgroupRequestCpu()
    {
        if (TryGetCgroupRequestCpu(_fileSystem, out var cpuPodRequest))
        {
            return cpuPodRequest / CpuShares;
        }

        return cpuPodRequest;
    }

    /// <remarks>
    /// If the file doesn't exist, we assume that the system is a Host and we read the memory from /proc/meminfo.
    /// </remarks>
    public ulong GetAvailableMemoryInBytes()
    {
        const long UnsetCgroupMemoryLimit = 9_223_372_036_854_771_712;

        if (!_fileSystem.Exists(_memoryLimitInBytes))
        {
            return GetHostAvailableMemory();
        }

        _fileSystem.ReadAll(_memoryLimitInBytes, _buffer);

        var memoryBuffer = _buffer.WrittenSpan;
        _ = GetNextNumber(memoryBuffer, out var maybeMemory);

        if (maybeMemory == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_memoryLimitInBytes}' content. Expected to find available memory in bytes but got '{new string(memoryBuffer)}' instead.");
        }

        _buffer.Reset();

        return maybeMemory == UnsetCgroupMemoryLimit
            ? GetHostAvailableMemory()
            : (ulong)maybeMemory;
    }

    public long GetMemoryUsageInBytesFromSlices()
    {
        string[] memoryUsageInBytesSlicesPath = Directory.GetDirectories(@"/sys/fs/cgroup", "*.slice", SearchOption.TopDirectoryOnly);
        long memoryUsageInBytesTotal = 0;

        foreach (string path in memoryUsageInBytesSlicesPath)
        {
            var memoryUsageInBytesFile = new FileInfo(path + "/memory.current");
            if (!_fileSystem.Exists(memoryUsageInBytesFile))
            {
                continue;
            }

            _fileSystem.ReadAll(memoryUsageInBytesFile, _buffer);

            var memoryUsageFile = _buffer.WrittenSpan;
            var next = GetNextNumber(memoryUsageFile, out var containerMemoryUsage);

            // this file format doesn't expect to contain anything after the number.
            if (containerMemoryUsage == -1)
            {
                Throw.InvalidOperationException(
                    $"We tried to read '{memoryUsageInBytesFile}', and we expected to get a positive number but instead it was: '{containerMemoryUsage}'.");
            }

            memoryUsageInBytesTotal += containerMemoryUsage;

            _buffer.Reset();
        }

        return memoryUsageInBytesTotal;
    }

    /// <remarks>
    /// If the file doesn't exist, we assume that the system is a Host and we read the memory from /proc/meminfo.
    /// </remarks>
    public ulong GetMemoryUsageInBytes()
    {
        const string InactiveFile = "inactive_file";

        if (!_fileSystem.Exists(_memoryStat))
        {
            return GetHostAvailableMemory();
        }

        _fileSystem.ReadAll(_memoryStat, _buffer);
        var memoryFile = _buffer.WrittenSpan;

        var index = memoryFile.IndexOf(InactiveFile.AsSpan());

        if (index == -1)
        {
            Throw.InvalidOperationException($"Unable to find inactive_file from '{_memoryStat}'.");
        }

        var inactiveMemorySlice = memoryFile.Slice(index + InactiveFile.Length, memoryFile.Length - index - InactiveFile.Length);

        _ = GetNextNumber(inactiveMemorySlice, out var inactiveMemory);

        if (inactiveMemory == -1)
        {
            Throw.InvalidOperationException($"The value of inactive_file found in '{_memoryStat}' is not a positive number: '{new string(inactiveMemorySlice)}'.");
        }

        _buffer.Reset();

        long memoryUsage = 0;
        if (!_fileSystem.Exists(_memoryUsageInBytes))
        {
            memoryUsage = GetMemoryUsageInBytesFromSlices();
        }
        else
        {
            memoryUsage = GetMemoryUsageInBytesPod();
        }

        if (memoryUsage < 0)
        {
            Throw.InvalidOperationException($"The total memory usage read from '{_memoryUsageInBytes}' is lesser than inactive memory usage read from '{_memoryStat}'.");
        }

        var memoryUsageTotal = memoryUsage - inactiveMemory;

        return (ulong)memoryUsageTotal;
    }

    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Shifting bits left by number n is multiplying the value by 2 to the power of n.")]
    public ulong GetHostAvailableMemory()
    {
        // The value we are interested in starts with this. We just want to make sure it is true.
        const string MemTotal = "MemTotal:";

        _fileSystem.ReadFirstLine(_memInfo, _buffer);
        var firstLine = _buffer.WrittenSpan;

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

        _buffer.Reset();

        return u;
    }

    /// <remarks>
    /// Comma-separated list of integers, with dashes ("-") to represent ranges. For example "0-1,5", or "0", or "1,2,3".
    /// Each value represents the zero-based index of a CPU.
    /// </remarks>
    public float GetHostCpuCount()
    {
        _fileSystem.ReadFirstLine(_cpuSetCpus, _buffer);
        var stats = _buffer.WrittenSpan;

        if (stats.IsEmpty)
        {
            ThrowException(stats);
        }

        var cpuCount = 0L;

        // Iterate over groups (comma-separated)
        while (true)
        {
            var groupIndex = stats.IndexOf(',');

            var group = groupIndex == -1 ? stats : stats.Slice(0, groupIndex);

            var rangeIndex = group.IndexOf('-');

            if (rangeIndex == -1)
            {
                // Single number
                _ = GetNextNumber(group, out var singleCpu);

                if (singleCpu == -1)
                {
                    ThrowException(stats);
                }

                cpuCount += 1;
            }
            else
            {
                // Range
                var first = group.Slice(0, rangeIndex);
                _ = GetNextNumber(first, out var startCpu);

                var second = group.Slice(rangeIndex + 1);
                var next = GetNextNumber(second, out var endCpu);

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

        _buffer.Reset();

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

    /// <remarks>
    /// If the file doesn't exist, we assume that the system is a Host and we read the CPU usage from /proc/stat.
    /// </remarks>
    private bool TryGetCpuUnitsFromCgroups(IFileSystem fileSystem, out float cpuUnits)
    {
        if (!_fileSystem.Exists(_cpuCfsQuaotaPeriodUs))
        {
            cpuUnits = 0;
            return false;
        }

        fileSystem.ReadFirstLine(_cpuCfsQuaotaPeriodUs, _buffer);

        var quotaBuffer = _buffer.WrittenSpan;

        if (quotaBuffer.IsEmpty || (quotaBuffer.Length == 2 && quotaBuffer[0] == '-' && quotaBuffer[1] == '1'))
        {
            _buffer.Reset();
            cpuUnits = -1;
            return false;
        }

        var nextQuota = GetNextNumber(quotaBuffer, out var quota);

        if (quota == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsQuaotaPeriodUs}'. Expected an integer but got: '{new string(quotaBuffer)}'.");
        }

        var quotaString = quota.ToString(CultureInfo.CurrentCulture);
        var index = quotaBuffer.IndexOf(quotaString.AsSpan());
        var cpuPeriodSlice = quotaBuffer.Slice(index + quotaString.Length, quotaBuffer.Length - index - quotaString.Length);
        _ = GetNextNumber(cpuPeriodSlice, out var period);

        if (period == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuCfsQuaotaPeriodUs}'. Expected to get an integer but got: '{new string(cpuPeriodSlice)}'.");
        }

        _buffer.Reset();
        cpuUnits = (float)quota / period;

        return true;
    }

    private bool TryGetCgroupRequestCpu(IFileSystem fileSystem, out float cpuUnits)
    {
        // If the file doesn't exist, we assume that the system is a Host and we assign the default 1 CPU core.
        if (!_fileSystem.Exists(_cpuPodWeight))
        {
            cpuUnits = 1;
            Throw.InvalidOperationException($"'{_cpuPodWeight}' file does not exist!");
            return false;
        }

        fileSystem.ReadFirstLine(_cpuPodWeight, _buffer);
        var cpuPodWeightBuffer = _buffer.WrittenSpan;

        if (cpuPodWeightBuffer.IsEmpty || (cpuPodWeightBuffer.Length == 2 && cpuPodWeightBuffer[0] == '-' && cpuPodWeightBuffer[1] == '1'))
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuPodWeight}' content. Expected to find CPU weight but got '{new string(cpuPodWeightBuffer)}' instead.");
            _buffer.Reset();
            cpuUnits = -1;
            return false;
        }

        _ = GetNextNumber(cpuPodWeightBuffer, out var cpuPodWeight);

        if (cpuPodWeight == -1)
        {
            Throw.InvalidOperationException($"Could not parse '{_cpuPodWeight}'. Expected to get an integer but got: '{cpuPodWeight}'.");
        }

        // Calculate CPU pod request in millicores based on the weight, using the formula:
        // y = (1 + ((x - 2) * 9999) / 262142), where y is the CPU weight and x is the CPU share (cgroup v1)
        // https://github.com/kubernetes/enhancements/tree/master/keps/sig-node/2254-cgroup-v2#phase-1-convert-from-cgroups-v1-settings-to-v2
        var cpuPodShare = ((cpuPodWeight * 262142) + 19997) / 9999;
        if (cpuPodShare == -1)
        {
            Throw.InvalidOperationException($"Could not calculate CPU share from CPU weight '{cpuPodShare}'");
            _buffer.Reset();
            cpuUnits = -1;
            return false;
        }

        _buffer.Reset();
        cpuUnits = cpuPodShare;
        return true;
    }

    private long GetMemoryUsageInBytesPod()
    {
        _fileSystem.ReadAll(_memoryUsageInBytes, _buffer);

        var memoryUsageFile = _buffer.WrittenSpan;
        var next = GetNextNumber(memoryUsageFile, out long memoryUsage);

        // this file format doesn't expect to contain anything after the number.
        if (memoryUsage == -1)
        {
            Throw.InvalidOperationException(
                $"We tried to read '{_memoryUsageInBytes}', and we expected to get a positive number but instead it was: '{memoryUsage}'.");
        }

        _buffer.Reset();
        return memoryUsage;
    }
}

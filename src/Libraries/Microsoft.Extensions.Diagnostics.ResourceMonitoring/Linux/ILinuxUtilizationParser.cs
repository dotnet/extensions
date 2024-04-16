// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

/// <summary>
/// An interface to be implemented by a parser that reads files and extracts resource utilization data from them.
/// For both versions of cgroup controllers, the parser reads files from the /sys/fs/cgroup directory.
/// </summary>
internal interface ILinuxUtilizationParser
{
    /// <summary>
    /// Reads file /sys/fs/cgroup/memory.max, which  is used to check the maximum amount of memory that can be used by a cgroup.
    /// It is part of the cgroup v2 memory controller.
    /// </summary>
    /// <returns>maybeMemory.</returns>
    ulong GetAvailableMemoryInBytes();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/cpu.stat, which is part of the cgroup v2 CPU controller.
    /// It provides statistics about the CPU usage of a cgroup.
    /// </summary>
    /// <returns>nanoseconds.</returns>
    long GetCgroupCpuUsageInNanoseconds();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/cpu.max, which is part of the cgroup v2 CPU controller.
    /// It is used to set the maximum amount of CPU time that can be used by a cgroup.
    /// The file contains two fields, separated by a space.
    /// The first field is the quota, which specifies the maximum amount of CPU time (in microseconds) that can be used by the cgroup during one period.
    /// The second value is the period, which specifies the length of a period in microseconds.
    /// </summary>
    /// <returns>cpuUnits.</returns>
    float GetCgroupLimitedCpus();

    /// <summary>
    /// Reads the file /proc/stat, which  provides information about the system’s memory usage.
    /// It contains information about the total amount of installed memory, the amount of free and used memory, and the amount of memory used by the kernel and buffers/cache.
    /// </summary>
    /// <returns>memory.</returns>
    ulong GetHostAvailableMemory();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/cpuset.cpus.effective, which is part of the cgroup v2 cpuset controller.
    /// It shows the effective cpus that the cgroup can use. 
    /// </summary>
    /// <returns>cpuCount.</returns>
    float GetHostCpuCount();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/cpu.stat, which is part of the cgroup v2 CPU controller.
    /// It provides statistics about the CPU usage of a cgroup.
    /// The file contains several fields, including usage_usec, which shows the total CPU time (in microseconds).
    /// </summary>
    /// <returns>total / (double)_userHz * NanosecondsInSecond.</returns>
    long GetHostCpuUsageInNanoseconds();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/memory.current, which is a file that contains the current memory usage of a cgroup in bytes.
    /// </summary>
    /// <returns>memoryUsage.</returns>
    ulong GetMemoryUsageInBytes();

    /// <summary>
    /// Reads the file /sys/fs/cgroup/cpu.weight. And calculates the Pod CPU Request in millicores.
    /// </summary>
    /// <returns>cpuPodRequest.</returns>
    float GetCgroupRequestCpu();
}

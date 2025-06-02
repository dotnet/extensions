// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

[SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Generators.")]
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Generators.")]
internal static partial class Log
{
    [LoggerMessage(1, LogLevel.Debug,
#pragma warning disable S103 // Lines should not be too long
        "Computed CPU usage with CgroupCpuTime = {cgroupCpuTime}, HostCpuTime = {hostCpuTime}, PreviousCgroupCpuTime = {previousCgroupCpuTime}, PreviousHostCpuTime = {previousHostCpuTime}, CpuPercentage = {cpuPercentage}.")]
#pragma warning restore S103 // Lines should not be too long
    public static partial void CpuUsageData(
        ILogger logger,
        long cgroupCpuTime,
        long hostCpuTime,
        long previousCgroupCpuTime,
        long previousHostCpuTime,
        double cpuPercentage);

    [LoggerMessage(2, LogLevel.Debug,
        "Computed memory usage with MemoryUsedInBytes = {memoryUsed}, MemoryLimit = {memoryLimit}, MemoryPercentage = {memoryPercentage}.")]
    public static partial void MemoryUsageData(
        ILogger logger,
        ulong memoryUsed,
        double memoryLimit,
        double memoryPercentage);

    [LoggerMessage(3, LogLevel.Debug,
        "System resources information: CpuLimit = {cpuLimit}, CpuRequest = {cpuRequest}, MemoryLimit = {memoryLimit}, MemoryRequest = {memoryRequest}.")]
    public static partial void SystemResourcesInfo(ILogger logger, double cpuLimit, double cpuRequest, ulong memoryLimit, ulong memoryRequest);

    [LoggerMessage(4, LogLevel.Debug,
#pragma warning disable S103 // Lines should not be too long
        "For CgroupV2, Computed CPU usage with CgroupCpuTime = {cgroupCpuTime}, PreviousCgroupCpuTime = {previousCgroupCpuTime}, ActualElapsedNanoseconds = {actualElapsedNanoseconds}, CpuCores = {cpuCores}.")]
#pragma warning restore S103 // Lines should not be too long
    public static partial void CpuUsageDataV2(
        ILogger logger,
        long cgroupCpuTime,
        long previousCgroupCpuTime,
        double actualElapsedNanoseconds,
        double cpuCores);

    [LoggerMessage(5, LogLevel.Debug,
        "CPU utilization exceeded 100%: Counter = {counterValue}")]
    public static partial void CounterMessage100(
        ILogger logger,
        long counterValue);

    [LoggerMessage(6, LogLevel.Debug,
        "CPU utilization exceeded 110%: Counter = {counterValue}")]
    public static partial void CounterMessage110(
        ILogger logger,
        long counterValue);

    [LoggerMessage(7, LogLevel.Warning,
        "Error while getting disk stats: Error={errorMessage}")]
    public static partial void HandleDiskStatsException(ILogger logger, string errorMessage);
}

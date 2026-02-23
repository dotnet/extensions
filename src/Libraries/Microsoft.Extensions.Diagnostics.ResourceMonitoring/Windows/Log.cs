// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal static partial class Log
{
    [LoggerMessage(1, LogLevel.Information, "Resource Monitoring is running inside a Job Object. For more information about Job Objects see https://aka.ms/job-objects")]
    public static partial void RunningInsideJobObject(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Resource Monitoring is running outside of Job Object. For more information about Job Objects see https://aka.ms/job-objects")]
    public static partial void RunningOutsideJobObject(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug,
        "Computed CPU usage with CpuUsageTicks = {cpuUsageTicks}, OldCpuUsageTicks = {oldCpuUsageTicks}, TimeTickDelta = {timeTickDelta}, CpuUnits = {cpuUnits}, CpuPercentage = {cpuPercentage}.")]
    public static partial void CpuUsageData(
        this ILogger logger,
        long cpuUsageTicks,
        long oldCpuUsageTicks,
        double timeTickDelta,
        double cpuUnits,
        double cpuPercentage);

    [LoggerMessage(4, LogLevel.Debug,
        "Computed memory usage for container: CurrentMemoryUsage = {currentMemoryUsage}, LimitMemory = {limitMemory}, RequestMemory = {requestMemory}.")]
    public static partial void ContainerMemoryUsageData(
        this ILogger logger,
        ulong currentMemoryUsage,
        double limitMemory,
        double requestMemory);

    [LoggerMessage(5, LogLevel.Debug, "Computed CPU usage with CpuUsageKernelTicks = {cpuUsageKernelTicks}, CpuUsageUserTicks = {cpuUsageUserTicks}, OldCpuUsageTicks = {oldCpuUsageTicks}, TimeTickDelta = {timeTickDelta}, CpuUnits = {cpuUnits}, CpuPercentage = {cpuPercentage}.")]
    public static partial void CpuContainerUsageData(
        this ILogger logger,
        long cpuUsageKernelTicks,
        long cpuUsageUserTicks,
        long oldCpuUsageTicks,
        double timeTickDelta,
        double cpuUnits,
        double cpuPercentage);

    [LoggerMessage(6, LogLevel.Debug,
        "System resources information: CpuLimit = {cpuLimit}, CpuRequest = {cpuRequest}, MemoryLimit = {memoryLimit}, MemoryRequest = {memoryRequest}.")]
    public static partial void SystemResourcesInfo(
        this ILogger logger,
        double cpuLimit,
        double cpuRequest,
        ulong memoryLimit,
        ulong memoryRequest);

    [LoggerMessage(7, LogLevel.Warning,
        "Error initializing disk io perf counter: PerfCounter={counterName}, Error={errorMessage}")]
    public static partial void DiskIoPerfCounterException(
        this ILogger logger,
        string counterName,
        string errorMessage);

    [LoggerMessage(8, LogLevel.Debug,
        "Computed memory usage for current process: ProcessMemoryUsage = {processMemoryUsage}, TotalMemory = {totalMemory}, MemoryPercentage = {memoryPercentage}")]
    public static partial void ProcessMemoryPercentageData(
        this ILogger logger,
        ulong processMemoryUsage,
        double totalMemory,
        double memoryPercentage);
}

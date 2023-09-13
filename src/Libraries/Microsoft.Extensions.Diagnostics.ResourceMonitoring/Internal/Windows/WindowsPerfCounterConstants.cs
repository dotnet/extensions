// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Performance counter constants.
/// </summary>
internal static class WindowsPerfCounterConstants
{
    /// <summary>
    /// The container counter category name.
    /// </summary>
    public const string ContainerCounterCategoryName = "COSMIC Containers";

    /// <summary>
    /// The container counter category label.
    /// </summary>
    public const string ContainerCounterCategoryLabel = "Container counters";

    /// <summary>
    /// The cpu utilization percentage.
    /// </summary>
    public const string CpuUtilizationCounterName = "% CPU Limit Utilization";

    /// <summary>
    /// The memory utilization percentage.
    /// </summary>
    public const string MemoryUtilizationCounterName = "% Memory Limit Utilization";

    /// <summary>
    /// The memory limit.
    /// </summary>
    public const string MemoryLimitCounterName = "Available Memory Limit";
}

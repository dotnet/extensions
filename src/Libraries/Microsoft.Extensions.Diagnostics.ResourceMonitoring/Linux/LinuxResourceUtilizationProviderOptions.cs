// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options for Linux resource utilization provider.
/// </summary>
public class LinuxResourceUtilizationProviderOptions
{
    internal const int MinimumCachingInterval = 100;
    internal const int MaximumCachingInterval = 900000; // 15 minutes.
    internal static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="LinuxResourceUtilizationCounters.CpuConsumptionPercentage"/>.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(MinimumCachingInterval, MaximumCachingInterval)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; } = DefaultRefreshInterval;

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="LinuxResourceUtilizationCounters.MemoryConsumptionPercentage"/>.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(MinimumCachingInterval, MaximumCachingInterval)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; } = DefaultRefreshInterval;
}

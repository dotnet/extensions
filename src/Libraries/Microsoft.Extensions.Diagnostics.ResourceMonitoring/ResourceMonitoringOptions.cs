// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options to control resource monitoring behavior.
/// </summary>
public partial class ResourceMonitoringOptions
{
    internal const int MinimumSamplingWindow = 100;
    internal const int MaximumSamplingWindow = 900000; // 15 minutes.
    internal const int MinimumSamplingPeriod = 1;
    internal const int MaximumSamplingPeriod = 900000; // 15 minutes.
    internal const int MinimumCachingInterval = 100;
    internal const int MaximumCachingInterval = 900000; // 15 minutes.
    internal static readonly TimeSpan DefaultCollectionWindow = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan DefaultSamplingInterval = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum time window for which utilization can be requested.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This value represents the total amount of time for which the resource monitor tracks utilization
    /// information for the system.
    /// </remarks>
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    [TimeSpan(MinimumSamplingWindow, MaximumSamplingWindow)]
    public TimeSpan CollectionWindow { get; set; } = DefaultCollectionWindow;

    /// <summary>
    /// Gets or sets the interval at which a new utilization sample is captured.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    /// <remarks>
    /// This value must be less than or equal to <see cref="CollectionWindow"/>.
    /// </remarks>
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    [TimeSpan(MinimumSamplingPeriod, MaximumSamplingPeriod)]
    public TimeSpan SamplingInterval { get; set; } = DefaultSamplingInterval;

    /// <summary>
    /// Gets or sets the observation window used to calculate the <see cref="ResourceUtilization"/> instances pushed to publishers.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// The value needs to be less than or equal to <see cref="CollectionWindow"/>.
    /// </remarks>
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    [TimeSpan(MinimumSamplingWindow, MaximumSamplingWindow)]
    public TimeSpan PublishingWindow { get; set; } = DefaultCollectionWindow;

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <c>"process.cpu.utilization"</c> metrics.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    [TimeSpan(MinimumCachingInterval, MaximumCachingInterval)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; } = DefaultRefreshInterval;

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <c>"dotnet.process.memory.virtual.utilization"</c> metrics.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    [TimeSpan(MinimumCachingInterval, MaximumCachingInterval)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; } = DefaultRefreshInterval;

    /// <summary>
    /// Gets or sets a value indicating whether CPU metrics are calculated via cgroup CPU limits instead of Host CPU delta.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.ResourceMonitoring, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool CalculateCpuUsageWithoutHostDelta { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the number of periods in cpu.stat for cgroup CPU usage.
    /// We use delta time for CPU usage calculation when this flag is not set.
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    ///  </summary>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.ResourceMonitoring, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool UseDeltaNrPeriodsForCpuCalculation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether disk I/O metrics should be enabled.
    /// </summary>
    /// <remarks>Previously <c>EnableDiskIoMetrics</c>.</remarks>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.ResourceMonitoring, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool EnableSystemDiskIoMetrics { get; set; }
}

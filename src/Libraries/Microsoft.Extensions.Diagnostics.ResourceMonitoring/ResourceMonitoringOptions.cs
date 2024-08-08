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
    internal static readonly TimeSpan DefaultCollectionWindow = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan DefaultSamplingInterval = TimeSpan.FromSeconds(1);

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
    [TimeSpan(MinimumSamplingWindow, MaximumSamplingWindow)]
    public TimeSpan PublishingWindow { get; set; } = DefaultCollectionWindow;

    /// <summary>
    /// Gets or sets a value indicating whether to use container metric names "container.*" instead of process metric names "process.*.".
    /// </summary>
    /// <remarks>
    /// Container metric names is the way forward and will be the default in the future.
    /// </remarks>
    [Experimental(DiagnosticIds.Experiments.ResourceMonitoring, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool UseContainerMetricNames { get; set; }
}

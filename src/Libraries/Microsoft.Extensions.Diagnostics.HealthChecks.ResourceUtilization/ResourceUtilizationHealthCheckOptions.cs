// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the resource utilization health check.
/// </summary>
public class ResourceUtilizationHealthCheckOptions
{
    internal const int MinimumSamplingWindow = 100;
    internal static readonly TimeSpan DefaultSamplingWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets thresholds for CPU utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by the Resource Monitoring library.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds CpuThresholds { get; set; } = new ResourceUsageThresholds();

    /// <summary>
    /// Gets or sets thresholds for memory utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by the Resource Monitoring library.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds MemoryThresholds { get; set; } = new ResourceUsageThresholds();

    /// <summary>
    /// Gets or sets the time window used for calculating CPU and memory utilization averages.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
#pragma warning disable CS0436 // Type conflicts with imported type    
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
#pragma warning restore CS0436 // Type conflicts with imported type    
    [TimeSpan(MinimumSamplingWindow, int.MaxValue)]
    public TimeSpan SamplingWindow { get; set; } = DefaultSamplingWindow;

    /// <summary>
    /// Gets or sets a value indicating whether the observable instruments will be used for getting CPU and Memory usage
    /// as opposed to the default <see cref="Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor"/> API which is obsolete.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the observable instruments are used. The default is <see langword="false" />.
    /// In the future the default will be <see langword="true" />.
    /// </value>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.HealthChecks, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool UseObservableResourceMonitoringInstruments { get; set; }
}

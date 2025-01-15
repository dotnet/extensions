// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public partial class ResourceMonitoringOptions
{
    /// <summary>
    /// Gets or sets the list of source IPv4 addresses to track the connections for in telemetry.
    /// </summary>
    /// <remarks>
    /// This property is Windows-specific and has no effect on other operating systems.
    /// </remarks>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
    public ISet<string> SourceIpAddresses { get; set; } = new HashSet<string>();
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets a value indicating whether CPU and Memory utilization metric values should be in range <c>[0, 1]</c> instead of <c>[0, 100]</c>.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Use this property if you prefer to have the metric values in range <c>[0, 1]</c> instead of <c>[0, 100]</c>.
    /// In the long term, the default value of this property will be changed to <see langword="true"/>.
    /// </remarks>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.ResourceMonitoring, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool UseZeroToOneRangeForMetrics { get; set; }
}

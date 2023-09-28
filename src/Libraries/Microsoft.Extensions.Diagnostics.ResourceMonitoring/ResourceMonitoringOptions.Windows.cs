// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
}

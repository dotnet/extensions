// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options for WindowsCounters.
/// </summary>
[Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class WindowsCountersOptions
{
    /// <summary>
    /// Gets or sets the list of source IPv4 addresses to track the connections for in telemetry.
    /// </summary>
    [Required]
    public ISet<string> InstanceIpAddresses { get; set; }

    /// <summary>
    /// Gets or sets the default interval used for freshing TcpStateInfo Cache.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [TimeSpan(100, 900000)]
    public TimeSpan CachingInterval { get; set; }

    public WindowsCountersOptions();
}

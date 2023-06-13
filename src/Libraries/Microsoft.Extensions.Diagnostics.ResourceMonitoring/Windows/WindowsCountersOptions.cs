// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options for WindowsCounters.
/// </summary>
[Experimental]
public class WindowsCountersOptions
{
    internal const int MinimumCachingInterval = 100;
    internal const int MaximumCachingInterval = 900000; // 15 minutes.
    internal static readonly TimeSpan DefaultCachingInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the list of source IPv4 addresses to track the connections for in telemetry.
    /// </summary>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
    public ISet<string> InstanceIpAddresses { get; set; } = new HashSet<string>();
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets the default interval used for freshing TcpStateInfo Cache.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [Experimental]
    [TimeSpan(MinimumCachingInterval, MaximumCachingInterval)]
    public TimeSpan CachingInterval { get; set; } = DefaultCachingInterval;
}

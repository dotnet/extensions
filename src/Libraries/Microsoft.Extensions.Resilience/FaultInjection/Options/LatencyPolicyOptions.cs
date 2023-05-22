// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for latency policy options definition.
/// </summary>
public class LatencyPolicyOptions : ChaosPolicyOptionsBase
{
    internal static readonly TimeSpan DefaultLatency = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the latency value to inject.
    /// </summary>
    /// <remarks>
    /// The value should be between 0 seconds to 10 minutes.
    /// Default is set to 30 seconds.
    /// </remarks>
    [TimeSpan("00:00:00", "00:10:00")]
    public TimeSpan Latency { get; set; } = DefaultLatency;
}

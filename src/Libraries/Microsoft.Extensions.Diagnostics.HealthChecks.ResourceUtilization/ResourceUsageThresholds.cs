// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Threshold settings for <see cref="ResourceUtilizationHealthCheckOptions"/>.
/// </summary>
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "In place numbers make the ranges cleaner")]
public class ResourceUsageThresholds
{
    /// <summary>
    /// Gets or sets the percentage threshold for the degraded state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [Range(0.0, 100.0)]
    public double? DegradedUtilizationPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage threshold for the unhealthy state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [Range(0.0, 100.0)]
    public double? UnhealthyUtilizationPercentage { get; set; }
}

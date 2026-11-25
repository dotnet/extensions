// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Threshold settings for <see cref="NetworkUtilizationHealthCheckOptions"/>.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.HealthChecks, UrlFormat = DiagnosticIds.UrlFormat)]
public class ResourceCountThresholds
{
    /// <summary>
    /// Gets or sets the count threshold for the degraded state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    [Range(typeof(long), "0", "9223372036854775807")]
#pragma warning restore IL2026
    public long? DegradedCount { get; set; }

    /// <summary>
    /// Gets or sets the count threshold for the unhealthy state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    [Range(typeof(long), "0", "9223372036854775807")]
#pragma warning restore IL2026
    public long? UnhealthyCount { get; set; }
}

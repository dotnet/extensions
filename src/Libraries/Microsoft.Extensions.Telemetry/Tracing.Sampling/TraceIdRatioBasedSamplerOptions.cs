// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for the trace Id ratio based sampler.
/// </summary>
public class TraceIdRatioBasedSamplerOptions
{
    /// <summary>
    /// Gets or sets the desired probability of sampling.
    /// </summary>
    /// <value>The default is 1.</value>
    [Range(0.0, 1.0)]
    public double Probability { get; set; } = 1.0;
}

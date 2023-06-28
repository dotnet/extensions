// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for sampling.
/// </summary>
public class SamplingOptions
{
    /// <summary>
    /// Gets or sets the type of the sampler.
    /// </summary>
    /// <remarks>
    /// Defaults to the <see cref="SamplerType.AlwaysOn"/> sampler.
    /// </remarks>
    public SamplerType SamplerType { get; set; } = SamplerType.AlwaysOn;

    /// <summary>
    /// Gets or sets options for the parent based sampler.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public ParentBasedSamplerOptions? ParentBasedSamplerOptions { get; set; }

    /// <summary>
    /// Gets or sets options for the trace Id ratio based sampler.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }
}

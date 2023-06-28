// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for the parent based sampler.
/// </summary>
public class ParentBasedSamplerOptions
{
    /// <summary>
    /// Gets or sets the type of sampler to be used for making sampling decision for root activity.
    /// </summary>
    /// <remarks>
    /// Defaults to the <see cref="SamplerType.AlwaysOn"/> sampler.
    /// </remarks>
    public SamplerType RootSamplerType { get; set; } = SamplerType.AlwaysOn;

    /// <summary>
    /// Gets or sets options for the trace Id ratio based sampler.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }
}

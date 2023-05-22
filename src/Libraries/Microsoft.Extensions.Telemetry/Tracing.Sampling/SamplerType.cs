// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Sampler type.
/// </summary>
public enum SamplerType
{
    /// <summary>
    /// Always samples traces.
    /// </summary>
    AlwaysOn,

    /// <summary>
    /// Never samples traces.
    /// </summary>
    AlwaysOff,

    /// <summary>
    /// Samples traces according to the specified probability.
    /// </summary>
    TraceIdRatioBased,

    /// <summary>
    /// Samples traces if the parent Activity is sampled.
    /// </summary>
    ParentBased
}

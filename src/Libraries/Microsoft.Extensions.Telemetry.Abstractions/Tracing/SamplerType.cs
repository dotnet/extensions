// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Sampler type.
/// </summary>
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
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
    RatioBased
}

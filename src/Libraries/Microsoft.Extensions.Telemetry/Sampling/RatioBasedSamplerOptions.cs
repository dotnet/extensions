// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// The options for ratio based sampling.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class RatioBasedSamplerOptions
{
    /// <summary>
    /// Gets or sets the collection of <see cref="RatioBasedSamplerFilterRule"/> used for filtering log messages.
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists - List is necessary to be able to call .AddRange()
#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern
    public List<RatioBasedSamplerFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
}

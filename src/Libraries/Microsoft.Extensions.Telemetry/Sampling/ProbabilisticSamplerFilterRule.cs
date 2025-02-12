// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Defines a rule used to filter log messages for purposes of sampling.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class ProbabilisticSamplerFilterRule : ILoggerSamplerFilterRule
{
    /// <inheritdoc/>
    public string? Category { get; set; }

    /// <inheritdoc/>
    public LogLevel? LogLevel { get; set; }

    /// <inheritdoc/>
    public int? EventId { get; set; }

    /// <summary>
    /// Gets or sets the probability for sampling in if this rule applies.
    /// </summary>
    public double Probability { get; set; }
}

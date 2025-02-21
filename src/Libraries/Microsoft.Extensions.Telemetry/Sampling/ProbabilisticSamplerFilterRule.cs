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
public class ProbabilisticSamplerFilterRule : ILogSamplingFilterRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProbabilisticSamplerFilterRule"/> class.
    /// </summary>
    /// <param name="probability">The probability for sampling in if this rule applies.</param>
    /// <param name="categoryName">The category name to use in this filter rule.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
    /// <param name="eventId">The event ID to use in this filter rule.</param>
    /// <param name="eventName">The event name to use in this filter rule.</param>
    public ProbabilisticSamplerFilterRule(
        double probability,
        string? categoryName = null,
        LogLevel? logLevel = null,
        int? eventId = null,
        string? eventName = null)
    {
        Probability = probability;
        CategoryName = categoryName;
        LogLevel = logLevel;
        EventId = eventId;
        EventName = eventName;
    }

    /// <summary>
    /// Gets the probability for sampling in if this rule applies.
    /// </summary>
    public double Probability { get; }

    /// <inheritdoc/>
    public string? CategoryName { get; }

    /// <inheritdoc/>
    public LogLevel? LogLevel { get; }

    /// <inheritdoc/>
    public int? EventId { get; }

    /// <inheritdoc/>
    public string? EventName { get; }
}

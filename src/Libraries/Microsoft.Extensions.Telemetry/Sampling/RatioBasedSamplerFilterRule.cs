// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Defines a rule used to filter log messages for purposes of sampling.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class RatioBasedSamplerFilterRule : ILoggerSamplerFilterRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RatioBasedSamplerFilterRule"/> class.
    /// </summary>
    public RatioBasedSamplerFilterRule()
        : this(1.0, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RatioBasedSamplerFilterRule"/> class.
    /// </summary>
    /// <param name="probability">The probability for sampling in if this rule applies.</param>
    /// <param name="categoryName">The category name to use in this filter rule.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
    /// <param name="eventId">The <see cref="EventId"/> to use in this filter rule.</param>
    /// <param name="filter">The filter to apply.</param>
    public RatioBasedSamplerFilterRule(
        double probability,
        string? categoryName,
        LogLevel? logLevel,
        int? eventId,
        Func<string?, LogLevel?, int?, bool>? filter)
    {
        Probability = probability;
        Category = categoryName;
        LogLevel = logLevel;
        EventId = eventId;
        Filter = filter;
    }

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

    /// <inheritdoc/>
    public Func<string?, LogLevel?, int?, bool>? Filter { get; }
}

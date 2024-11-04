// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Defines a rule used to filter log messages for purposes of sampling.
/// </summary>
public class RatioBasedSamplerFilterRule : ILoggerSamplerFilterRule
{
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
        EventId? eventId,
        Func<string?, LogLevel, EventId?, bool>? filter)
    {
        Probability = probability;
        CategoryName = categoryName;
        LogLevel = logLevel;
        EventId = eventId;
        Filter = filter;
    }

    /// <summary>
    /// Gets the logger category this rule applies to.
    /// </summary>
    public string? CategoryName { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages.
    /// </summary>
    public LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the <see cref="EventId"/> of messages this rule applies to.
    /// </summary>
    public EventId? EventId { get; }

    /// <summary>
    /// Gets the probability for sampling in if this rule applies.
    /// </summary>
    public double Probability { get; }

    /// <summary>
    /// Gets the filter delegate that would be applied to messages that passed the <see cref="LogLevel"/>.
    /// </summary>
    public Func<string?, LogLevel, EventId?, bool>? Filter { get; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Defines a rule used to filter log messages.
/// </summary>
public class LoggerFilterRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerFilterRule"/> class.
    /// </summary>
    /// <param name="categoryName">The category name to use in this filter rule.</param>
    /// <param name="eventId">The event ID to use in this filter rule.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
    /// <param name="filter">The filter to apply.</param>
    public LoggerFilterRule(
        string? categoryName,
        EventId? eventId,
        LogLevel? logLevel,
        Func<string?, EventId?, LogLevel?, bool>? filter)
    {
        CategoryName = categoryName;
        EventId = eventId;
        LogLevel = logLevel;
        Filter = filter;
    }

    /// <summary>
    /// Gets the logger category this rule applies to.
    /// </summary>
    public string? CategoryName { get; }

    /// <summary>
    /// Gets the event ID this rules applies to.
    /// </summary>
    public EventId? EventId { get; }

    /// <summary>
    /// Gets the minimum <see cref="LogLevel"/> of messages.
    /// </summary>
    public LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the filter delegate that would be applied to logs.
    /// </summary>
    public Func<string?, EventId?, LogLevel?, bool>? Filter { get; }
}

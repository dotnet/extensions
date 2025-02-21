// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Defines a rule used to filter log messages for purposes of further buffering.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class LogBufferingFilterRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogBufferingFilterRule"/> class.
    /// </summary>
    /// <param name="categoryName">The category name to use in this filter rule.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
    /// <param name="eventId">The event ID to use in this filter rule.</param>
    /// <param name="eventName">The event name to use in this filter rule.</param>
    /// <param name="attributes">The log state attributes to use in this filter tule.</param>
    public LogBufferingFilterRule(
        string? categoryName = null,
        LogLevel? logLevel = null,
        int? eventId = null,
        string? eventName = null,
        IReadOnlyList<KeyValuePair<string, object?>>? attributes = null)
    {
        CategoryName = categoryName;
        LogLevel = logLevel;
        EventId = eventId;
        EventName = eventName;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the logger category name this rule applies to.
    /// </summary>
    public string? CategoryName { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages this rule applies to.
    /// </summary>
    public LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the evnet ID of messages where this rule applies to.
    /// </summary>
    public int? EventId { get; }

    /// <summary>
    /// Gets the name of the event this rule applies to.
    /// </summary>
    public string? EventName { get; }

    /// <summary>
    /// Gets the log state attributes of messages where this rules applies to.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>>? Attributes { get; }
}

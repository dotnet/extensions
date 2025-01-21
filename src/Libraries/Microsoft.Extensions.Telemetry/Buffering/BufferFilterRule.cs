// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Defines a rule used to filter log messages for purposes of futher buffering.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class BufferFilterRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BufferFilterRule"/> class.
    /// </summary>
    public BufferFilterRule()
        : this(null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferFilterRule"/> class.
    /// </summary>
    /// <param name="categoryName">The category name to use in this filter rule.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use in this filter rule.</param>
    /// <param name="eventId">The <see cref="EventId"/> to use in this filter rule.</param>
    /// <param name="attributes">The optional attributes to use if a log message passes other filters.</param>
    public BufferFilterRule(string? categoryName, LogLevel? logLevel, int? eventId,
        IReadOnlyList<KeyValuePair<string, object?>>? attributes = null)
    {
        Category = categoryName;
        LogLevel = logLevel;
        EventId = eventId;
        Attributes = attributes ?? [];
    }

    /// <summary>
    /// Gets or sets the logger category this rule applies to.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the maximum <see cref="LogLevel"/> of messages.
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="EventId"/> of messages where this rule applies to.
    /// </summary>
    public int? EventId { get; set; }

    /// <summary>
    /// Gets or sets the log state attributes of messages where this rules applies to.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; set; }
}

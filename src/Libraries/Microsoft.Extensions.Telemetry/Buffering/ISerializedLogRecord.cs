// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Represents a buffered log record that has been serialized.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface ISerializedLogRecord
{
    /// <summary>
    /// Gets the time when the log record was first created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the record's logging severity level.
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// Gets the record's event id.
    /// </summary>
    public EventId EventId { get; }

    /// <summary>
    /// Gets an exception string for this record.
    /// </summary>
    public string? Exception { get; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string? FormattedMessage { get; }

    /// <summary>
    /// Gets the variable set of name/value pairs associated with the record.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }
}

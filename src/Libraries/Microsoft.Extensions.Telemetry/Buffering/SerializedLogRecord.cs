// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Represents a log record that has been serialized for purposes of buffering or similar.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly struct SerializedLogRecord
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedLogRecord"/> struct.
    /// </summary>
    /// <param name="logLevel">Logging severity level.</param>
    /// <param name="eventId">Event ID.</param>
    /// <param name="timestamp">The time when the log record was first created.</param>
    /// <param name="attributes">The set of name/value pairs associated with the record.</param>
    /// <param name="exception">An exception string for this record.</param>
    /// <param name="formattedMessage">The formatted log message.</param>
    public SerializedLogRecord(
        LogLevel logLevel,
        EventId eventId,
        DateTimeOffset timestamp,
        IReadOnlyList<KeyValuePair<string, object?>> attributes,
        Exception? exception,
        string formattedMessage)
    {
        LogLevel = logLevel;
        EventId = eventId;
        Timestamp = timestamp;

        List<KeyValuePair<string, object?>> serializedAttributes = [];
        if (attributes is not null)
        {
            serializedAttributes = new List<KeyValuePair<string, object?>>(attributes.Count);
            for (int i = 0; i < attributes.Count; i++)
            {
                string value = attributes[i].Value?.ToString() ?? string.Empty;
                serializedAttributes.Add(new KeyValuePair<string, object?>(attributes[i].Key, value));
                SizeInBytes += value.Length * sizeof(char);
            }

            Exception = exception?.Message;
        }

        Attributes = serializedAttributes;
        FormattedMessage = formattedMessage;
    }

    /// <summary>
    /// Gets the variable set of name/value pairs associated with the record.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string? FormattedMessage { get; }

    /// <summary>
    /// Gets an exception string for this record.
    /// </summary>
    public string? Exception { get; }

    /// <summary>
    /// Gets the time when the log record was first created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the record's logging severity level.
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// Gets the record's event ID.
    /// </summary>
    public EventId EventId { get; }

    /// <summary>
    /// Gets the approximate size of the serialized log record in bytes.
    /// </summary>
    public int SizeInBytes { get; init; }
}

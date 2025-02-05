// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if !SHARED_PROJECT || NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Represents a log record deserialized from somewhere, such as buffer.
/// </summary>
internal sealed class DeserializedLogRecord : BufferedLogRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeserializedLogRecord"/> class.
    /// </summary>
    /// <param name="timestamp">The time when the log record was first created.</param>
    /// <param name="logLevel">Logging severity level.</param>
    /// <param name="eventId">Event ID.</param>
    /// <param name="exception">An exception string for this record.</param>
    /// <param name="formattedMessage">The formatted log message.</param>
    /// <param name="attributes">The set of name/value pairs associated with the record.</param>
    public DeserializedLogRecord(
        DateTimeOffset timestamp,
        LogLevel logLevel,
        EventId eventId,
        string? exception,
        string? formattedMessage,
        IReadOnlyList<KeyValuePair<string, object?>> attributes)
    {
        _timestamp = timestamp;
        _logLevel = logLevel;
        _eventId = eventId;
        _exception = exception;
        _formattedMessage = formattedMessage;
        _attributes = attributes;
    }

    /// <inheritdoc/>
    public override DateTimeOffset Timestamp => _timestamp;
    private DateTimeOffset _timestamp;

    /// <inheritdoc/>
    public override LogLevel LogLevel => _logLevel;
    private LogLevel _logLevel;

    /// <inheritdoc/>
    public override EventId EventId => _eventId;
    private EventId _eventId;

    /// <inheritdoc/>
    public override string? Exception => _exception;
    private string? _exception;

    /// <inheritdoc/>
    public override string? FormattedMessage => _formattedMessage;
    private string? _formattedMessage;

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, object?>> Attributes => _attributes;
    private IReadOnlyList<KeyValuePair<string, object?>> _attributes;
}
#endif

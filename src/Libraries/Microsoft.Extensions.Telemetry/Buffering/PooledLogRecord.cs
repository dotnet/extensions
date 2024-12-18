// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Diagnostics.Buffering;
internal sealed class PooledLogRecord : BufferedLogRecord, IResettable
{
    public PooledLogRecord(
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

    public override DateTimeOffset Timestamp => _timestamp;
    private DateTimeOffset _timestamp;

    public override LogLevel LogLevel => _logLevel;
    private LogLevel _logLevel;

    public override EventId EventId => _eventId;
    private EventId _eventId;

    public override string? Exception => _exception;
    private string? _exception;

    public override ActivitySpanId? ActivitySpanId => _activitySpanId;
    private ActivitySpanId? _activitySpanId;

    public override ActivityTraceId? ActivityTraceId => _activityTraceId;
    private ActivityTraceId? _activityTraceId;

    public override int? ManagedThreadId => _managedThreadId;
    private int? _managedThreadId;

    public override string? FormattedMessage => _formattedMessage;
    private string? _formattedMessage;

    public override string? MessageTemplate => _messageTemplate;
    private string? _messageTemplate;

    public override IReadOnlyList<KeyValuePair<string, object?>> Attributes => _attributes;
    private IReadOnlyList<KeyValuePair<string, object?>> _attributes;

    public bool TryReset()
    {
        _timestamp = default;
        _logLevel = default;
        _eventId = default;
        _exception = default;
        _activitySpanId = default;
        _activityTraceId = default;
        _managedThreadId = default;
        _formattedMessage = default;
        _messageTemplate = default;
        _attributes = [];

        return true;
    }
}

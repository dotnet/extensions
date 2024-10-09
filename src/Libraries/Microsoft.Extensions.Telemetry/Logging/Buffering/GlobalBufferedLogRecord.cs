// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

internal class GlobalBufferedLogRecord : BufferedLogRecord
{
    public GlobalBufferedLogRecord(
        LogLevel logLevel,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> state,
        Exception? exception,
        string? formatter)
    {
        LogLevel = logLevel;
        EventId = eventId;
        Attributes = state; // TODO: should be serialized
        Exception = exception?.ToString(); // wtf??
        FormattedMessage = formatter;
    }

    public override IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; }
    public override string? FormattedMessage { get; }
    public override string? Exception { get; }

    public override DateTimeOffset Timestamp { get; }

    public override LogLevel LogLevel { get; }

    public override EventId EventId { get; }
}

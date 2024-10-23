// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

internal class HttpRequestBufferedLogRecord : GlobalBufferedLogRecord
{
    public HttpRequestBufferedLogRecord(
        LogLevel logLevel,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> state,
        Exception? exception,
        string? formatter)
        : base(logLevel, eventId, state, exception, formatter)
    {
        // just re-used the Global implementation for now
    }
}

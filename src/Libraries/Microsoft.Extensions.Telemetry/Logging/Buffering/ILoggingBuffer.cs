// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Interface for a logging buffer.
/// </summary>
public interface ILoggingBuffer
{
    /// <summary>
    /// Flushes the buffer and emits all buffered logs.
    /// </summary>
    void Flush();

    /// <summary>
    /// Checks if the buffer is enabled for the given set of parameters.
    /// </summary>
    /// <returns><see langword="true" /> if enabled.</returns>
    bool IsEnabled(string category, LogLevel logLevel, EventId eventId);

    /// <summary>
    /// Enqueues a log record.
    /// </summary>
    void Enqueue(LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>> joiner, Exception? exception, string formatter);
}

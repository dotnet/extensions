// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    /// Enqueues a log record.
    /// </summary>
    /// <returns>true or false.</returns>
    bool TryEnqueue(
        IBufferedLogger logger,
        string category,
        LogLevel logLevel,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> joiner,
        Exception? exception,
        string formatter);
}

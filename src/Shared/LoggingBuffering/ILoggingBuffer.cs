// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface for a logging buffer.
/// </summary>
internal interface ILoggingBuffer
{
    /// <summary>
    /// Enqueues a log record in the underlying buffer.
    /// </summary>
    /// <param name="logLevel">Log level.</param>
    /// <param name="category">Category.</param>
    /// <param name="eventId">Event ID.</param>
    /// <param name="state">Log state attributes.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="formatter">Formatter delegate.</param>
    /// <typeparam name="TState">Type of the <paramref name="state"/> instance.</typeparam>
    /// <returns><see langword="true"/> if the log record was buffered; otherwise, <see langword="false"/>.</returns>
    bool TryEnqueue<TState>(
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter);

    /// <summary>
    /// Flushes the buffer.
    /// </summary>
    void Flush();
}

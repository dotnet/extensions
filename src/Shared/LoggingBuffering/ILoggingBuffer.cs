// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface for a logging buffer.
/// </summary>
internal interface ILoggingBuffer
{
    /// <summary>
    /// Enqueues a log record in the underlying buffer.
    /// </summary>
    /// <typeparam name="TState">Type of the log state in the <paramref name="logEntry"/> instance.</typeparam>
    /// <returns><see langword="true"/> if the log record was buffered; otherwise, <see langword="false"/>.</returns>
    bool TryEnqueue<TState>(LogEntry<TState> logEntry);

    /// <summary>
    /// Flushes the buffer.
    /// </summary>
    void Flush();
}

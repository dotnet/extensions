// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Buffers logs into circular buffers and drops them after some time if not flushed.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
public abstract class LogBuffer
#pragma warning restore S1694 // An abstract class should have both abstract and concrete methods
{
    /// <summary>
    /// Flushes the buffer and emits all buffered logs.
    /// </summary>
    public abstract void Flush();

    /// <summary>
    /// Enqueues a log record in the underlying buffer, if available.
    /// </summary>
    /// <param name="bufferedLogger">A logger capable of logging buffered log records.</param>
    /// <param name="logEntry">A log entry to be buffered.</param>
    /// <typeparam name="TState">Type of the log state in the <paramref name="logEntry"/> instance.</typeparam>
    /// <returns><see langword="true"/> if the log record was buffered; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry);
}

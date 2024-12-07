// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface for a global buffer manager.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IBufferManager
{
    /// <summary>
    /// Flushes the buffer and emits all buffered logs.
    /// </summary>
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Logging.ILoggingBuffer.Flush()")]
    void Flush();

    /// <summary>
    /// Enqueues a log record in the underlying buffer.
    /// </summary>
    /// <param name="bufferSink">Buffer sink.</param>
    /// <param name="logLevel">Log level.</param>
    /// <param name="category">Category.</param>
    /// <param name="eventId">Event ID.</param>
    /// <param name="attributes">Attributes.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="formatter">Formatter delegate.</param>
    /// <typeparam name="TState">Type of the <paramref name="attributes"/> instance.</typeparam>
    /// <returns><see langword="true"/> if the log record was buffered; otherwise, <see langword="false"/>.</returns>
    [RequiresUnreferencedCode(
        "Calls Microsoft.Extensions.Logging.SerializedLogRecord.SerializedLogRecord(LogLevel, EventId, DateTimeOffset, IReadOnlyList<KeyValuePair<String, Object>>, Exception, String)")]
    bool TryEnqueue<TState>(
        IBufferSink bufferSink,
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState attributes,
        Exception? exception,
        Func<TState, Exception?, string> formatter);
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Interface for a logging buffer.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
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
        LogLevel logLevel,
        string category,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> joiner,
        Exception? exception,
        string formatter);
}
#endif

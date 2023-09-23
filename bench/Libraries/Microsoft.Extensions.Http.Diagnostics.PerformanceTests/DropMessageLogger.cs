// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Bench;

internal sealed class DropMessageLogger : ILogger
{
    internal static readonly Func<LogLevel, EventId, object, Exception, object?> CreateLogRecord
        = (_, _, _, _) => null;

#pragma warning disable CS8633
#pragma warning disable CS8766
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;
#pragma warning restore CS8633
#pragma warning restore CS8766

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        _ = CreateLogRecord(logLevel, eventId, state, exception);
#pragma warning restore CS8604 // Possible null reference argument.
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Extensions.AI;

internal sealed class CapturingLogger : ILogger
{
    private readonly Stack<LoggerScope> _scopes = new();
    private readonly List<LogEntry> _entries = [];
    private readonly LogLevel _enabledLevel;

    public CapturingLogger(LogLevel enabledLevel = LogLevel.Trace)
    {
        _enabledLevel = enabledLevel;
    }

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        var scope = new LoggerScope(this);
        _scopes.Push(scope);
        return scope;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _enabledLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        lock (_entries)
        {
            _entries.Add(new LogEntry(logLevel, eventId, state, exception, message));
        }
    }

    private sealed class LoggerScope(CapturingLogger owner) : IDisposable
    {
        public void Dispose() => owner.EndScope(this);
    }

    private void EndScope(LoggerScope loggerScope)
    {
        if (_scopes.Peek() != loggerScope)
        {
            throw new InvalidOperationException("Logger scopes out of order");
        }

        _scopes.Pop();
    }

    public record LogEntry(LogLevel Level, EventId EventId, object? State, Exception? Exception, string Message);
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public CapturingLogger Logger { get; } = new();

    public ILogger CreateLogger(string categoryName) => Logger;

    void IDisposable.Dispose()
    {
        // nop
    }
}

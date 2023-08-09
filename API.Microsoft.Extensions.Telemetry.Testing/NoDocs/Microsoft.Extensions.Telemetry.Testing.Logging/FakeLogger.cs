// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

public class FakeLogger : ILogger
{
    public FakeLogCollector Collector { get; }
    public FakeLogRecord LatestRecord { get; }
    public string? Category { get; }
    public FakeLogger(FakeLogCollector? collector = null, string? category = null);
    public FakeLogger(Action<string> outputSink, string? category = null);
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
    public void ControlLevel(LogLevel logLevel, bool enabled);
    public bool IsEnabled(LogLevel logLevel);
}

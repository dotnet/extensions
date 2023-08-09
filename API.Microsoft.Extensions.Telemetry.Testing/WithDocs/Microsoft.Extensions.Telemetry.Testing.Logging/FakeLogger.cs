// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

/// <summary>
/// A logger which captures everything logged to it and enables inspection.
/// </summary>
/// <remarks>
/// This type is intended for use in unit tests. It captures all the log state to memory and lets you inspect it
/// to validate that your code is logging what it should.
/// </remarks>
public class FakeLogger : ILogger
{
    /// <summary>
    /// Gets the logger collector associated with this logger, as specified when the logger was created.
    /// </summary>
    public FakeLogCollector Collector { get; }

    /// <summary>
    /// Gets the latest record logged to this logger.
    /// </summary>
    /// <remarks>
    /// This is a convenience property that merely returns the latest record from the underlying collector.
    /// </remarks>
    /// <exception cref="T:System.InvalidOperationException">No records have been captured.</exception>
    public FakeLogRecord LatestRecord { get; }

    /// <summary>
    /// Gets this logger's category, as specified when the logger was created.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Testing.Logging.FakeLogger" /> class.
    /// </summary>
    /// <param name="collector">Where to push all log state. If this is <see langword="null" /> then a fresh collector is allocated automatically.</param>
    /// <param name="category">The logger's category, which indicates the origin of the logger and is captured in each record.</param>
    public FakeLogger(FakeLogCollector? collector = null, string? category = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Testing.Logging.FakeLogger" /> class that copies all log records to the given output sink.
    /// </summary>
    /// <param name="outputSink">Where to emit individual log records.</param>
    /// <param name="category">The logger's category, which indicates the origin of the logger and is captured in each record.</param>
    public FakeLogger(Action<string> outputSink, string? category = null);

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>A disposable object that ends the logical operation scope on dispose.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull;

    /// <summary>
    /// Creates a new log record.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a string message of the state and exception.</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    /// <summary>
    /// Controls the enabled state of a log level.
    /// </summary>
    /// <param name="logLevel">The log level to affect.</param>
    /// <param name="enabled">Whether the log level is enabled or not.</param>
    public void ControlLevel(LogLevel logLevel, bool enabled);

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><see langword="true" /> if enabled; <see langword="false" /> otherwise.</returns>
    public bool IsEnabled(LogLevel logLevel);
}

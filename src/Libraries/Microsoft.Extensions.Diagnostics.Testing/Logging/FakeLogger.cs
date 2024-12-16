// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.ExceptionJsonConverter;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// A logger which captures everything logged to it and enables inspection.
/// </summary>
/// <remarks>
/// This type is intended for use in unit tests. It captures all the log state to memory and lets you inspect it
/// to validate that your code is logging what it should.
/// </remarks>
public class FakeLogger : ILogger, IBufferedLogger
{
    private readonly ConcurrentDictionary<LogLevel, bool> _disabledLevels = new();  // used as a set, the value is ignored

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogger"/> class.
    /// </summary>
    /// <param name="collector">Where to push all log state. If this is <see langword="null"/> then a fresh collector is allocated automatically.</param>
    /// <param name="category">The logger's category, which indicates the origin of the logger and is captured in each record.</param>
    public FakeLogger(FakeLogCollector? collector = null, string? category = null)
    {
        Collector = collector ?? new();
        Category = string.IsNullOrEmpty(category) ? null : category;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogger"/> class that copies all log records to the given output sink.
    /// </summary>
    /// <param name="outputSink">Where to emit individual log records.</param>
    /// <param name="category">The logger's category, which indicates the origin of the logger and is captured in each record.</param>
    public FakeLogger(Action<string> outputSink, string? category = null)
        : this(FakeLogCollector.Create(new FakeLogCollectorOptions { OutputSink = outputSink }), category)
    {
    }

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>A disposable object that ends the logical operation scope on dispose.</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => ScopeProvider.Push(state);

    /// <summary>
    /// Creates a new log record.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a string message of the state and exception.</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _ = Throw.IfNull(formatter);

        var l = new List<object?>();
        ScopeProvider.ForEachScope((scopeState, list) => list.Add(scopeState), l);

        var record = new FakeLogRecord(logLevel, eventId, ConsumeTState(state), exception, formatter(state, exception),
            l.ToArray(), Category, !_disabledLevels.ContainsKey(logLevel), Collector.TimeProvider.GetUtcNow());
        Collector.AddRecord(record);
    }

    /// <summary>
    /// Controls the enabled state of a log level.
    /// </summary>
    /// <param name="logLevel">The log level to affect.</param>
    /// <param name="enabled">Whether the log level is enabled or not.</param>
    public void ControlLevel(LogLevel logLevel, bool enabled) => _ = enabled ? _disabledLevels.TryRemove(logLevel, out _) : _disabledLevels.TryAdd(logLevel, false);

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><see langword="true"/> if enabled; <see langword="false"/> otherwise.</returns>
    public bool IsEnabled(LogLevel logLevel) => !_disabledLevels.ContainsKey(logLevel);

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
    /// <exception cref="InvalidOperationException">No records have been captured.</exception>
    public FakeLogRecord LatestRecord => Collector.LatestRecord;

    /// <summary>
    /// Gets this logger's category, as specified when the logger was created.
    /// </summary>
    public string? Category { get; }

    /// <inheritdoc/>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
    public void LogRecords(IEnumerable<BufferedLogRecord> records)
    {
        _ = Throw.IfNull(records);

        var l = new List<object?>();

        foreach (var rec in records)
        {
            var exception = rec.Exception is null ?
                null : JsonSerializer.Deserialize(rec.Exception, ExceptionJsonContext.Default.Exception);
            var record = new FakeLogRecord(rec.LogLevel, rec.EventId, ConsumeTState(rec.Attributes), exception, rec.FormattedMessage ?? string.Empty,
    l.ToArray(), Category, !_disabledLevels.ContainsKey(rec.LogLevel), rec.Timestamp);
            Collector.AddRecord(record);
        }
    }

    internal IExternalScopeProvider ScopeProvider { get; set; } = new LoggerExternalScopeProvider();

    private static object? ConsumeTState(object? state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> e)
        {
            var l = new List<KeyValuePair<string, string?>>();
            foreach (var pair in e)
            {
                if (pair.Value != null)
                {
                    l.Add(new KeyValuePair<string, string?>(pair.Key, ConvertToString(pair)));
                }
                else
                {
                    l.Add(new KeyValuePair<string, string?>(pair.Key, null));
                }
            }

            return l;
        }

        if (state == null)
        {
            return null;
        }

        // the best we can do here is stringify the thing
        return Convert.ToString(state, CultureInfo.InvariantCulture);

        static string? ConvertToString(KeyValuePair<string, object?> pair)
        {
            string? str;
            if (pair.Value is string s)
            {
                str = s;
            }
            else if (pair.Value is IEnumerable ve)
            {
                str = LoggerMessageHelper.Stringify(ve);
            }
            else
            {
                str = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
            }

            return str;
        }
    }
}

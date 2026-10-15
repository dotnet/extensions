// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

[Collection("Serial Tests")]
public static partial class ExtendedLoggerReentrancyTests
{
    private const string OuterCategory = "Outer";
    private const string NestedCategory = "Nested";

    [Theory]
    [CombinatorialData]
    public static void NestedLogFromProvider_PreservesBothRecords(bool extendedFactory, bool generatedOuter, bool generatedNested)
    {
        var reentrantCollector = new FakeLogCollector();
        var downstreamCollector = new FakeLogCollector();
        using var reentrantProvider = new ReentrantProvider(reentrantCollector);
        using var downstreamProvider = new FakeLoggerProvider(downstreamCollector);
        using var errors = new LoggingErrorListener();

        Action<ILoggingBuilder> configure = builder => builder.AddProvider(reentrantProvider).AddProvider(downstreamProvider);
        using ILoggerFactory factory = extendedFactory ? Utils.CreateLoggerFactory(configure) : LoggerFactory.Create(configure);

        var nestedLogger = factory.CreateLogger(NestedCategory);
        reentrantProvider.OnOuterRecord = () =>
        {
            if (generatedNested)
            {
                NestedRecord(nestedLogger, "nested-value");
            }
            else
            {
                nestedLogger.LogInformation("Nested record {value}", "nested-value");
            }
        };

        var outerLogger = factory.CreateLogger(OuterCategory);
        var exception = new InvalidOperationException("Outer failure");
        if (generatedOuter)
        {
            OuterRecord(outerLogger, exception, "outer-value");
        }
        else
        {
            outerLogger.LogError(exception, "Outer record {value}", "outer-value");
        }

        Assert.Empty(errors.Errors);
        AssertRecords(reentrantCollector, exception, extendedFactory);
        AssertRecords(downstreamCollector, exception, extendedFactory);
    }

    private static void AssertRecords(FakeLogCollector collector, Exception exception, bool extendedFactory)
    {
        var records = collector.GetSnapshot();
        Assert.Equal(2, records.Count);

        var nested = records[0];
        Assert.Equal(NestedCategory, nested.Category);
        Assert.Equal("Nested record nested-value", nested.Message);
        Assert.Null(nested.Exception);
        Assert.Equal(new[] { "value=nested-value", "{OriginalFormat}=Nested record {value}" }, GetTags(nested));

        var outer = records[1];
        Assert.Equal(OuterCategory, outer.Category);
        Assert.Equal("Outer record outer-value", outer.Message);
        Assert.Same(exception, outer.Exception);

        var expectedOuterTags = new List<string>();
        if (extendedFactory)
        {
            expectedOuterTags.Add($"exception.type={typeof(InvalidOperationException)}");
        }

        expectedOuterTags.Add("value=outer-value");
        expectedOuterTags.Add("{OriginalFormat}=Outer record {value}");
        Assert.Equal(expectedOuterTags, GetTags(outer));
    }

    private static IEnumerable<string> GetTags(FakeLogRecord record) => record.StructuredState!.Select(tag => $"{tag.Key}={tag.Value}");

    [LoggerMessage(LogLevel.Error, "Outer record {value}")]
    private static partial void OuterRecord(ILogger logger, Exception exception, string value);

    [LoggerMessage(LogLevel.Information, "Nested record {value}")]
    private static partial void NestedRecord(ILogger logger, string value);

    // Emits a log record of its own while handling a record of the outer category, before reading that record.
    private sealed class ReentrantProvider : ILoggerProvider
    {
        private readonly FakeLogCollector _collector;

        public ReentrantProvider(FakeLogCollector collector)
        {
            _collector = collector;
        }

        public Action? OnOuterRecord { get; set; }

        public ILogger CreateLogger(string categoryName) => new ReentrantLogger(this, new FakeLogger(_collector, categoryName));

        public void Dispose()
        {
            // Nothing to dispose.
        }

        private sealed class ReentrantLogger : ILogger
        {
            private readonly ReentrantProvider _provider;
            private readonly FakeLogger _logger;

            public ReentrantLogger(ReentrantProvider provider, FakeLogger logger)
            {
                _provider = provider;
                _logger = logger;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => _logger.BeginScope(state);

            public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (_logger.Category == OuterCategory)
                {
                    _provider.OnOuterRecord?.Invoke();
                }

                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }

    // Collects the exceptions that logging swallows and reports through its event source on the current thread.
    private sealed class LoggingErrorListener : EventListener
    {
        private readonly int _threadId = Environment.CurrentManagedThreadId;

        public LoggingErrorListener()
        {
            EnableEvents(LoggingEventSource.Instance, EventLevel.Error);
        }

        public List<string> Errors { get; } = [];

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (Environment.CurrentManagedThreadId == _threadId)
            {
                Errors.Add(eventData.Payload?[0]?.ToString() ?? string.Empty);
            }
        }
    }
}

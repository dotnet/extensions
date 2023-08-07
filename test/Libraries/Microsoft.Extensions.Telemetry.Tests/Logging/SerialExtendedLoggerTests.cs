// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Log;

[CollectionDefinition("Serial Tests", DisableParallelization = true)]
public static class SerialExtendedLoggerTests
{
    [Fact]
    public static void BadContributors()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var stackTraceOptions = new LoggerEnrichmentOptions();

        int eventCount = 0;
        using var el = new LoggerEventListener();
        el.EventWritten += (_, e) =>
        {
            var payload = e.Payload?[0];
            if (payload != null)
            {
                var s = payload.ToString();
                if (s != null && s.Contains("I'M ANGRY"))
                {
                    eventCount++;
                }
            }
        };

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(stackTraceOptions),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { new AngryEnricher() },
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: new FakeRedactorProvider(),
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), (LogMethodHelper?)null, null, (_, _) => "MSG1");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), (LoggerMessageState?)null, null, (_, _) => "MSG2");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(3, collector.Count);

        Assert.Equal(3, eventCount);
    }

    private sealed class Provider : ILoggerProvider
    {
        public FakeLogger? Logger { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            Logger = new FakeLogger((FakeLogCollector?)null, categoryName);
            return Logger;
        }

        public void Dispose()
        {
            // nothing to do
        }
    }

    private sealed class AngryEnricher : ILogEnricher, IStaticLogEnricher
    {
        public void Enrich(IEnrichmentTagCollector enrichmentPropertyBag)
        {
            throw new InvalidOperationException("I'M ANGRY!");
        }
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public IDisposable? OnChange(Action<T, string> listener) => null;
        public T Get(string? name) => CurrentValue;
        public T CurrentValue { get; }
    }

    private sealed class LoggerEventListener : EventListener
    {
        public LoggerEventListener()
        {
            EnableEvents(LoggingEventSource.Instance, EventLevel.Informational);
        }
    }
}

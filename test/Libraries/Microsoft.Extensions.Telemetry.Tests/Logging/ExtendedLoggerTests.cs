// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Log;

public static class ExtendedLoggerTests
{
    [Fact]
    public static void Basic()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var enricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("EK1", "EV1"),
            });

        var staticEnricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("SEK1", "SEV1"),
            });

        var redactorProvider = new FakeRedactorProvider(new FakeRedactorOptions
        {
            RedactionFormat = "REDACTED<{0}>",
        });

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { enricher },
            staticEnrichers: new[] { staticEnricher },
            redactorProvider: redactorProvider,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.LogWarning("MSG0");

        var lmh = LogMethodHelper.GetHelper();
        lmh.Add("PK1", "PV1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), lmh, null, (_, _) => "MSG1");

        var lms = LoggerMessageHelper.ThreadLocalState;
        var sp = lms.AllocPropertySpace(1);
        sp[0] = new("PK2", "PV2");

        var sp2 = lms.AllocClassifiedPropertySpace(1);
        sp2[0] = new("PK3", "PV3", SimpleClassifications.PrivateData);

        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), lms, null, (_, _) => "MSG2");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(3, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("EV1", snap[0].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[0].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[1].Category);
        Assert.Null(snap[1].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[1].Id);
        Assert.Equal("MSG1", snap[1].Message);
        Assert.Equal("PV1", snap[1].StructuredState!.GetValue("PK1"));
        Assert.Equal("EV1", snap[1].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[1].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[2].Id);
        Assert.Equal("MSG2", snap[2].Message);
        Assert.Equal("PV2", snap[2].StructuredState!.GetValue("PK2"));
        Assert.Equal("REDACTED<PV3>", snap[2].StructuredState!.GetValue("PK3"));
        Assert.Equal("EV1", snap[2].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[2].StructuredState!.GetValue("SEK1"));
    }

    [Fact]
    public static void NullStateObject()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var enricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("EK1", "EV1"),
            });

        var staticEnricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("SEK1", "SEV1"),
            });

        var redactorProvider = new FakeRedactorProvider(new FakeRedactorOptions
        {
            RedactionFormat = "REDACTED<{0}>",
        });

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { enricher },
            staticEnrichers: new[] { staticEnricher },
            redactorProvider: redactorProvider,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0b"), null, null, (_, _) => "MSG0b");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), (LogMethodHelper?)null, null, (_, _) => "MSG1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1b"), (LogMethodHelper?)null, null, (_, _) => "MSG1b");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), (LoggerMessageState?)null, null, (_, _) => "MSG2");
        logger.Log(LogLevel.Warning, new EventId(2, "ID2b"), (LoggerMessageState?)null, null, (_, _) => "MSG2b");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(6, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("EV1", snap[0].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[0].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[1].Category);
        Assert.Null(snap[1].Exception);
        Assert.Equal(new EventId(0, "ID0b"), snap[1].Id);
        Assert.Equal("MSG0b", snap[1].Message);
        Assert.Equal("EV1", snap[1].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[1].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[2].Id);
        Assert.Equal("MSG1", snap[2].Message);
        Assert.Equal("EV1", snap[2].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[2].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[3].Category);
        Assert.Null(snap[3].Exception);
        Assert.Equal(new EventId(1, "ID1b"), snap[3].Id);
        Assert.Equal("MSG1b", snap[3].Message);
        Assert.Equal("EV1", snap[3].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[3].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[4].Category);
        Assert.Null(snap[4].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[4].Id);
        Assert.Equal("MSG2", snap[4].Message);
        Assert.Equal("EV1", snap[4].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[4].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[5].Category);
        Assert.Null(snap[5].Exception);
        Assert.Equal(new EventId(2, "ID2b"), snap[5].Id);
        Assert.Equal("MSG2b", snap[5].Message);
        Assert.Equal("EV1", snap[5].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[5].StructuredState!.GetValue("SEK1"));
    }

    [Fact]
    public static void Exceptions()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var stackTraceOptions = new LoggerEnrichmentOptions
        {
            CaptureStackTraces = true,
            UseFileInfoForStackTraces = true,
            MaxStackTraceLength = 4096,
        };

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(stackTraceOptions),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: Array.Empty<ILogEnricher>(),
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: new FakeRedactorProvider(),
            scopeProvider: null,
            factoryOptions: null);

        Exception ex;
        try
        {
            List<Exception> exceptions = new();
            try
            {
                throw new ArgumentNullException("ARG1");
            }
            catch (ArgumentNullException e)
            {
                exceptions.Add(e);
            }

            try
            {
                throw new ArgumentOutOfRangeException("ARG2");
            }
            catch (ArgumentOutOfRangeException e)
            {
                exceptions.Add(e);
            }

            try
            {
                throw new InvalidOperationException("Doing crazy things");
            }
            catch (InvalidOperationException e)
            {
                exceptions.Add(e);
            }

            throw new AggregateException("Outer", exceptions);
        }
        catch (AggregateException e)
        {
            ex = e;
        }

        var logger = lf.CreateLogger(Category);
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0b"), null, ex, (_, _) => "MSG0b");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), (LogMethodHelper?)null, null, (_, _) => "MSG1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1b"), (LogMethodHelper?)null, ex, (_, _) => "MSG1b");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), (LoggerMessageState?)null, null, (_, _) => "MSG2");
        logger.Log(LogLevel.Warning, new EventId(2, "ID2b"), (LoggerMessageState?)null, ex, (_, _) => "MSG2b");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(6, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);

        Assert.Equal(Category, snap[1].Category);
        Assert.NotNull(snap[1].Exception);
        Assert.Equal(new EventId(0, "ID0b"), snap[1].Id);
        Assert.Equal("MSG0b", snap[1].Message);

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[2].Id);
        Assert.Equal("MSG1", snap[2].Message);

        Assert.Equal(Category, snap[3].Category);
        Assert.NotNull(snap[3].Exception);
        Assert.Equal(new EventId(1, "ID1b"), snap[3].Id);
        Assert.Equal("MSG1b", snap[3].Message);

        Assert.Equal(Category, snap[4].Category);
        Assert.Null(snap[4].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[4].Id);
        Assert.Equal("MSG2", snap[4].Message);

        Assert.Equal(Category, snap[5].Category);
        Assert.NotNull(snap[5].Exception);
        Assert.Equal(new EventId(2, "ID2b"), snap[5].Id);
        Assert.Equal("MSG2b", snap[5].Message);

        var stackTrace = snap[5].StructuredState!.GetValue("stackTrace")!;
        Assert.Contains("AggregateException", stackTrace);
        Assert.Contains("ArgumentNullException", stackTrace);
        Assert.Contains("ArgumentOutOfRangeException", stackTrace);
        Assert.Contains("InvalidOperationException", stackTrace);
    }

    [Fact]
    public static void Disabled()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var stackTraceOptions = new LoggerEnrichmentOptions();

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
        provider.Logger!.ControlLevel(LogLevel.Information, false);

        logger.Log<object?>(LogLevel.Information, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Information, new EventId(1, "ID1"), (LogMethodHelper?)null, null, (_, _) => "MSG1");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Debug, new EventId(2, "ID2"), (LoggerMessageState?)null, null, (_, _) => "MSG2");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(0, collector.Count);
    }

    private static string? GetValue(this IReadOnlyList<KeyValuePair<string, string>> state, string name)
    {
        foreach (var kvp in state)
        {
            if (kvp.Key == name)
            {
                return kvp.Value;
            }
        }

        return null;
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

    private sealed class ForcedEnricher : ILogEnricher, IStaticLogEnricher
    {
        private readonly KeyValuePair<string, object?>[] _values;

        public ForcedEnricher(KeyValuePair<string, object?>[] values)
        {
            _values = values;
        }

        public void Enrich(IEnrichmentPropertyBag enrichmentPropertyBag)
        {
            foreach (var kvp in _values)
            {
                enrichmentPropertyBag.Add(kvp.Key, kvp.Value!);
            }
        }
    }

    private sealed class AngryEnricher : ILogEnricher, IStaticLogEnricher
    {
        public void Enrich(IEnrichmentPropertyBag enrichmentPropertyBag)
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
}

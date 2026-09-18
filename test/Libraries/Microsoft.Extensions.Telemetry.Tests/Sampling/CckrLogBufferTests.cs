// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Test;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class CckrLogBufferTests
{
    private static readonly Func<IReadOnlyList<KeyValuePair<string, object?>>, Exception?, string> _formatter =
        static (_, _) => "message";

    [Fact]
    public void Admit_WhenIntervalElapsed_FlushesBeforeNewAdmission()
    {
        var timeProvider = new TestTimeProvider();
        var destination = new RecordingBufferedLogger();
        using var buffer = CreateBuffer(timeProvider);

        Enqueue(buffer, destination, [new("{OriginalFormat}", "message")]);
        Assert.Empty(destination.Records);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        _ = buffer.Admit("category", new EventId(1));

        Assert.Single(destination.Records);
    }

    [Fact]
    public void Dispose_FlushesRetainedRecords()
    {
        var destination = new RecordingBufferedLogger();
        var buffer = CreateBuffer(new TestTimeProvider());

        Enqueue(buffer, destination, [new("{OriginalFormat}", "message")]);
        buffer.Dispose();

        Assert.Single(destination.Records);
    }

    [Fact]
    public void Flush_AddsSamplingCountBeforeOriginalFormat()
    {
        var destination = new RecordingBufferedLogger();
        using var buffer = CreateBuffer(new TestTimeProvider());

        Enqueue(
            buffer,
            destination,
            [
                new("property", 42),
                new("{OriginalFormat}", "message {property}"),
            ]);
        buffer.Flush();

        BufferedLogRecord record = Assert.Single(destination.Records);
        Assert.Equal("sampling.count", record.Attributes[^2].Key);
        Assert.Equal("{OriginalFormat}", record.Attributes[^1].Key);
        Assert.True(double.IsFinite(Assert.IsType<double>(record.Attributes[^2].Value)));
    }

    [Fact]
    public void LoggingPipeline_PreservesEnrichmentAndSupportsOrdinaryProvider()
    {
        var provider = new CapturingProvider();
        using ILoggerFactory factory = Utils.CreateLoggerFactory(builder =>
        {
            builder.AddProvider(provider);
            builder.Services.AddSingleton<ILogEnricher>(new TestEnricher());
            builder.AddCckrLogSampling(options =>
            {
                options.Capacity = 1;
                options.PreserveCapacity = 0;
            });
        });

        ILogger logger = factory.CreateLogger("category");
        logger.LogInformation("message {property}", 42);

        var disposingFactory = Assert.IsType<Utils.DisposingLoggerFactory>(factory);
        disposingFactory.ServiceProvider.GetRequiredService<LogBuffer>().Flush();

        IReadOnlyList<KeyValuePair<string, object?>> state = Assert.Single(provider.States);
        Assert.Contains(state, pair => pair.Key == "enriched" && Equals(pair.Value, "value"));
        Assert.Contains(state, pair => pair.Key == "sampling.count" && pair.Value is double weight && weight >= 1.0);
        Assert.Equal("{OriginalFormat}", state[^1].Key);
    }

    private static CckrLogBuffer CreateBuffer(TimeProvider timeProvider)
        => new(
            new ReservoirSamplingConfig
            {
                Capacity = 1,
                PreserveCapacity = 0,
                FlushInterval = TimeSpan.FromSeconds(1),
            },
            timeProvider);

    private static void Enqueue(
        CckrLogBuffer buffer,
        IBufferedLogger destination,
        IReadOnlyList<KeyValuePair<string, object?>> state)
    {
        var eventId = new EventId(1);
        Assert.True(buffer.Admit("category", eventId));

        var entry = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Information,
            "category",
            eventId,
            state,
            null,
            _formatter);

        Assert.True(buffer.TryEnqueue(destination, entry));
    }

    private sealed class RecordingBufferedLogger : IBufferedLogger
    {
        public List<BufferedLogRecord> Records { get; } = [];

        public void LogRecords(IEnumerable<BufferedLogRecord> records)
            => Records.AddRange(records);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class TestEnricher : ILogEnricher
    {
        public void Enrich(IEnrichmentTagCollector collector)
            => collector.Add("enriched", "value");
    }

    private sealed class CapturingProvider : ILoggerProvider
    {
        public List<IReadOnlyList<KeyValuePair<string, object?>>> States { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingProvider _provider;

            public CapturingLogger(CapturingProvider provider)
            {
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (state is IReadOnlyList<KeyValuePair<string, object?>> attributes)
                {
                    _provider.States.Add(attributes);
                }
            }
        }
    }
}
#endif

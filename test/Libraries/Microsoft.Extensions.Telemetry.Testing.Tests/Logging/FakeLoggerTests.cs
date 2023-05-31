// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Logging.Test;

public class FakeLoggerTests
{
    [Fact]
    public void Basic()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new FakeLogCollectorOptions
        {
            TimeProvider = timeProvider,
        };

        var collector = FakeLogCollector.Create(options);
        var logger = new FakeLogger(collector);
        logger.LogInformation("Hello");
        logger.LogError("World");

        var records = logger.Collector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Equal(2, logger.Collector.Count);

        Assert.Equal("Hello", records[0].Message);
        Assert.Equal(LogLevel.Information, records[0].Level);
        Assert.Null(records[0].Exception);
        Assert.Null(records[0].Category);
        Assert.True(records[0].LevelEnabled);
        Assert.Empty(records[0].Scopes);
        Assert.Equal(0, records[0].Id.Id);
        Assert.Equal("[00:00.000,  info] Hello", records[0].ToString());

        Assert.Equal("World", records[1].Message);
        Assert.Equal(LogLevel.Error, records[1].Level);
        Assert.Null(records[0].Exception);
        Assert.Null(records[0].Category);
        Assert.Empty(records[0].Scopes);
        Assert.True(records[0].LevelEnabled);
        Assert.Equal(0, records[0].Id.Id);

        Assert.Equal("World", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Null(logger.LatestRecord.Category);
        Assert.True(records[0].LevelEnabled);
        Assert.Empty(logger.LatestRecord.Scopes);
        Assert.Equal(0, logger.LatestRecord.Id.Id);

        logger.Collector.Clear();
        Assert.Equal(0, logger.Collector.Count);
        Assert.Empty(logger.Collector.GetSnapshot());
        Assert.Throws<InvalidOperationException>(() => logger.LatestRecord.Level);
    }

    [Fact]
    public void State()
    {
        var logger = new FakeLogger();
        logger.Log<int>(LogLevel.Error, new EventId(0), 42, null, (_, _) => "MESSAGE");
        Assert.Equal("42", (string)logger.LatestRecord.State!);

        logger = new FakeLogger();

        var l = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("K0", "V0"),
            new KeyValuePair<string, object?>("K1", "V1"),
            new KeyValuePair<string, object?>("K2", null),
            new KeyValuePair<string, object?>("K3", new[] { 0, 1, 2 }),
        };

        logger.Log(LogLevel.Debug, new EventId(1), l, null, (_, _) => "Nothing");

        Assert.Equal(1, logger.Collector.Count);

        var stateList = (List<KeyValuePair<string, string>>)logger.LatestRecord.State!;
        Assert.Equal("K0", stateList[0].Key);
        Assert.Equal("V0", stateList[0].Value);
        Assert.Equal("K1", stateList[1].Key);
        Assert.Equal("V1", stateList[1].Value);
        Assert.Equal("K2", stateList[2].Key);
        Assert.Null(stateList[2].Value);
        Assert.Equal("K3", stateList[3].Key);
        Assert.Equal("[\"0\",\"1\",\"2\"]", stateList[3].Value);

        logger = new FakeLogger();
        logger.Log<object?>(LogLevel.Error, new EventId(0), null, null, (_, _) => "MESSAGE");
        Assert.Null(logger.LatestRecord.State);

        logger = new FakeLogger();
        TestLog.Hello(logger, "Bob");
        var ss = logger.LatestRecord.StructuredState!;
        Assert.Equal("name", ss[0].Key);
        Assert.Equal("Bob", ss[0].Value);
        Assert.Equal("{OriginalFormat}", ss[1].Key);
        Assert.Equal("Hello {name}", ss[1].Value);
    }

    [Fact]
    public void StateInvariant()
    {
        var oldCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-CA");

        var dt = new DateTime(2022, 5, 22);

        try
        {
            var logger = new FakeLogger();
            logger.Log(LogLevel.Error, new EventId(0), dt, null, (_, _) => "MESSAGE");
            Assert.Equal(dt.ToString(CultureInfo.InvariantCulture), (string)logger.LatestRecord.State!);

            var l = new List<KeyValuePair<string, object?>>
            {
                new KeyValuePair<string, object?>("K0", dt),
            };

            logger.Log(LogLevel.Debug, new EventId(1), l, null, (_, _) => "Nothing");
            Assert.Equal(dt.ToString(CultureInfo.InvariantCulture), logger.LatestRecord.StructuredState![0].Value!);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = oldCulture;
        }
    }

    [Fact]
    public void EnableControl()
    {
        var logger = new FakeLogger();

        Assert.True(logger.IsEnabled(LogLevel.Trace));
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
        Assert.True(logger.IsEnabled((LogLevel)42));

        logger.ControlLevel(LogLevel.Debug, false);
        logger.ControlLevel((LogLevel)42, false);

        Assert.True(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
        Assert.False(logger.IsEnabled((LogLevel)42));

        logger.LogDebug("This record should be marked as being disabled");
        Assert.Equal(1, logger.Collector.Count);
        Assert.False(logger.LatestRecord.LevelEnabled);

        logger.ControlLevel(LogLevel.Debug, true);

        logger.LogDebug("This record should be marked as being enabled");
        Assert.Equal(2, logger.Collector.Count);
        Assert.True(logger.LatestRecord.LevelEnabled);
    }

    [Fact]
    public void FilterByEnabled()
    {
        var options = new FakeLogCollectorOptions
        {
            CollectRecordsForDisabledLogLevels = false
        };
        var collector = FakeLogCollector.Create(options);
        var logger = new FakeLogger(collector);

        logger.LogDebug("BEFORE");
        logger.ControlLevel(LogLevel.Debug, false);
        logger.LogDebug("AFTER");

        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("BEFORE", logger.LatestRecord.Message);
    }

    [Fact]
    public void FilterByLevel()
    {
        var options = new FakeLogCollectorOptions
        {
            FilteredLevels = new HashSet<LogLevel>()
        };
        options.FilteredLevels.Add(LogLevel.Error);

        var collector = FakeLogCollector.Create(options);
        var logger = new FakeLogger(collector);

        logger.LogDebug("M1");
        logger.LogInformation("M2");
        logger.LogWarning("M3");
        logger.LogError("M4");
        logger.LogCritical("M5");

        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("M4", logger.LatestRecord.Message);
    }

    [Fact]
    public void FilterByCategory()
    {
        var options = new FakeLogCollectorOptions
        {
            FilteredCategories = new HashSet<string>()
        };
        options.FilteredCategories.Add("Storage");

        var collector = FakeLogCollector.Create(options);

        var logger = new FakeLogger(collector, category: null);
        logger.LogDebug("M1");

        logger = new FakeLogger(collector, "Network");
        logger.LogDebug("M1");

        logger = new FakeLogger(collector, "Storage");
        logger.LogDebug("M2");

        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("M2", logger.LatestRecord.Message);
    }

    [Fact]
    public void Clock()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new FakeLogCollectorOptions
        {
            TimeProvider = timeProvider,
        };

        var start = timeProvider.GetUtcNow();
        var collector = FakeLogCollector.Create(options);

        var logger = new FakeLogger(collector);
        logger.LogDebug("M1");
        logger.LogDebug("M2");

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        logger.LogDebug("M3");
        logger.LogDebug("M4");

        var records = collector.GetSnapshot();
        Assert.Equal(start, records[0].Timestamp);
        Assert.Equal(start, records[1].Timestamp);
        Assert.Equal(start + TimeSpan.FromMilliseconds(1), records[2].Timestamp);
        Assert.Equal(start + TimeSpan.FromMilliseconds(1), records[3].Timestamp);
    }

    [Fact]
    public void Scopes()
    {
        var logger = new FakeLogger();

        using var s1 = logger.BeginScope(42);
        using var s2 = logger.BeginScope("Hello World");
        logger.LogInformation("Main message");

        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal(2, logger.LatestRecord.Scopes.Count);
        Assert.Equal(42, (int)logger.LatestRecord.Scopes[0]!);
        Assert.Equal("Hello World", (string)logger.LatestRecord.Scopes[1]!);
    }
}

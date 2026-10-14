// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LogRateLimitTests
{
    private const string DroppedCounterName = "logging.rate_limiting.dropped";
    private const string EventNameTag = "logging.rate_limiting.event_name";

    [Fact]
    public void LogsUpToPermitLimit()
    {
        using var logger = Utils.GetLogger();

        for (int i = 0; i < 5; i++)
        {
            RateLimitTestExtensions.Limited(logger, $"arg{i}");
        }

        var records = logger.FakeLogCollector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Equal("Limited arg0", records[0].Message);
        Assert.Equal("Limited arg1", records[1].Message);
    }

    [Fact]
    public void DisabledLoggerDoesNotUseUpLimit()
    {
        using var logger = Utils.GetLogger();

        logger.FakeLogger.ControlLevel(LogLevel.Error, false);
        for (int i = 0; i < 5; i++)
        {
            RateLimitTestExtensions.LimitedError(logger, "disabled");
        }

        Assert.Equal(0, logger.FakeLogCollector.Count);

        logger.FakeLogger.ControlLevel(LogLevel.Error, true);
        for (int i = 0; i < 5; i++)
        {
            RateLimitTestExtensions.LimitedError(logger, "enabled");
        }

        Assert.Equal(2, logger.FakeLogCollector.Count);
        Assert.All(logger.FakeLogCollector.GetSnapshot(), r => Assert.Equal("LimitedError enabled", r.Message));
    }

    [Fact]
    public void CountsDroppedCallsOnMeter()
    {
        using var logger = Utils.GetLogger();
        using var meter = new Meter(nameof(CountsDroppedCallsOnMeter));
        using var metrics = new MetricCollector<long>(meter, DroppedCounterName);

        for (int i = 0; i < 4; i++)
        {
            RateLimitTestExtensions.LimitedWithMeter(logger, meter, "x");
        }

        var record = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.Equal("LimitedWithMeter x", record.Message);
        Assert.Null(record.GetStructuredStateValue("meter"));

        var measurements = metrics.GetMeasurementSnapshot();
        Assert.Equal(3, measurements.Sum(m => m.Value));
        Assert.All(measurements, m => Assert.Equal(nameof(RateLimitTestExtensions.LimitedWithMeter), m.Tags[EventNameTag]));
    }

    [Fact]
    public void NullMeterIsIgnored()
    {
        using var logger = Utils.GetLogger();

        RateLimitTestExtensions.LimitedWithNullMeter(logger, null, "a");
        RateLimitTestExtensions.LimitedWithNullMeter(logger, null, "b");

        var record = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.Equal("LimitedWithNullMeter a", record.Message);
    }

    [Fact]
    public void DroppedCounterUsesEventName()
    {
        using var logger = Utils.GetLogger();
        using var meter = new Meter(nameof(DroppedCounterUsesEventName));
        using var metrics = new MetricCollector<long>(meter, DroppedCounterName);

        RateLimitTestExtensions.LimitedWithEventName(logger, "a", meter);
        RateLimitTestExtensions.LimitedWithEventName(logger, "b", meter);

        Assert.Equal(1, logger.FakeLogCollector.Count);
        var measurement = Assert.Single(metrics.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("RateLimitedEventName", measurement.Tags[EventNameTag]);
    }

    [Fact]
    public void SkipEnabledCheckStillLimits()
    {
        using var logger = Utils.GetLogger();

        RateLimitTestExtensions.LimitedSkipEnabledCheck(logger);
        RateLimitTestExtensions.LimitedSkipEnabledCheck(logger);

        Assert.Equal(1, logger.FakeLogCollector.Count);
    }

    [Fact]
    public void OverloadsHaveSeparateLimits()
    {
        using var logger = Utils.GetLogger();

        RateLimitTestExtensions.Overload(logger, "a");
        RateLimitTestExtensions.Overload(logger, "b");
        RateLimitTestExtensions.Overload(logger, "c", 1);
        RateLimitTestExtensions.Overload(logger, "d", 2);

        var records = logger.FakeLogCollector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Equal("Overload a", records[0].Message);
        Assert.Equal("Overload c 1", records[1].Message);
    }

    [Fact]
    public void InstanceMethodIsLimited()
    {
        using var logger = Utils.GetLogger();
        var instance = new RateLimitInstanceTestClass(logger);

        instance.Instance("a");
        instance.Instance("b");

        var record = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.Equal("Instance a", record.Message);
    }
}

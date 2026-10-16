// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public class LoggerMessageRateLimiterTests
{
    // With this frequency, one timestamp tick is one millisecond.
    private const long Frequency = 1000;
    private const string EventName = "TestEvent";
    private const string DroppedCounterName = "logging.rate_limiting.dropped";
    private const string EventNameTag = "logging.rate_limiting.event_name";

    [Fact]
    public void OneSegment_AllowsPermitLimitPerWindow()
    {
        var clock = new FakeClock();
        var limiter = Create(3, 1000, 1, clock);

        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Now = 999;
        Assert.False(limiter.TryAcquire());

        clock.Now = 1000;
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void Segments_ReleaseCallsWhenTheyLeaveTheWindow()
    {
        var clock = new FakeClock();

        // 4 calls per second, in segments of 250 ms.
        var limiter = Create(4, 1000, 4, clock);

        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());

        clock.Now = 500;
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Now = 999;
        Assert.False(limiter.TryAcquire());

        // The segment that started at 0 left the window, so its 2 calls no longer count.
        clock.Now = 1000;
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        // The segment that started at 250 was empty.
        clock.Now = 1250;
        Assert.False(limiter.TryAcquire());

        // The segment that started at 500 left the window.
        clock.Now = 1500;
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void LongPause_ReleasesAllCalls()
    {
        var clock = new FakeClock();
        var limiter = Create(2, 1000, 4, clock);

        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Now = 60_000;
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void StaleTimestamp_CountsInTheLatestSegment()
    {
        var clock = new FakeClock();
        var limiter = Create(2, 1000, 1, clock);

        clock.Now = 1000;
        Assert.True(limiter.TryAcquire());

        // A call whose timestamp was read before another call moved the window forward.
        clock.Now = 0;
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Now = 1000;
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void SegmentNumberWrapsAround()
    {
        var clock = new FakeClock();

        // Segments of 1 ms, so the segment number is the timestamp.
        var limiter = Create(1, 1, 1, clock);

        const long HalfSegmentRange = 1L << 39;

        clock.Now = HalfSegmentRange - 1;
        Assert.True(limiter.TryAcquire());

        clock.Now = (2 * HalfSegmentRange) - 2;
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        // The segment number wraps around to 0.
        clock.Now = 2 * HalfSegmentRange;
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void ConcurrentCalls_NeverExceedPermitLimit()
    {
        var limiter = new LoggerMessageRateLimiter(1000, int.MaxValue, 1, EventName);
        int acquired = 0;

        Parallel.For(0, 100_000, _ =>
        {
            if (limiter.TryAcquire())
            {
                _ = Interlocked.Increment(ref acquired);
            }
        });

        Assert.Equal(1000, acquired);
    }

    [Theory]
    [InlineData(0, 1000, 1)]
    [InlineData(16_777_216, 1000, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1000, 0)]
    [InlineData(1, 10, 11)]
    public void Constructor_ThrowsOnInvalidLimit(int permitLimit, int windowMilliseconds, int segmentsPerWindow)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LoggerMessageRateLimiter(permitLimit, windowMilliseconds, segmentsPerWindow, EventName));
    }

    [Fact]
    public void Constructor_ThrowsOnNullEventName()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggerMessageRateLimiter(1, 1000, 1, null!));
    }

    [Fact]
    public void TryAcquireWithMeter_CountsDroppedCalls()
    {
        using var meter = new Meter(nameof(TryAcquireWithMeter_CountsDroppedCalls));
        using var listener = new DroppedCounterListener(meter);
        var limiter = Create(1, 1000, 1, new FakeClock());

        Assert.True(limiter.TryAcquire(meter));
        Assert.False(limiter.TryAcquire(meter));
        Assert.False(limiter.TryAcquire(meter));

        var instrument = Assert.Single(listener.Instruments);
        Assert.Equal("{log_record}", instrument.Unit);
        Assert.Equal(new (long, string?)[] { (1, EventName), (1, EventName) }, listener.Measurements);
    }

    [Fact]
    public void TryAcquireWithNullMeter_DropsWithoutCounting()
    {
        var limiter = Create(1, 1000, 1, new FakeClock());

        Assert.True(limiter.TryAcquire(null));
        Assert.False(limiter.TryAcquire(null));
    }

    [Fact]
    public void TryAcquireWithMeter_RecordsNothingWithoutListener()
    {
        using var meter = new Meter(nameof(TryAcquireWithMeter_RecordsNothingWithoutListener));
        var limiter = Create(1, 1000, 1, new FakeClock());

        Assert.True(limiter.TryAcquire(meter));
        Assert.False(limiter.TryAcquire(meter));

        // A listener that starts later sees the counter but no earlier measurements.
        using var listener = new DroppedCounterListener(meter);
        Assert.Single(listener.Instruments);
        Assert.Empty(listener.Measurements);
    }

    [Fact]
    public void TryAcquireWithMeter_SharesOneCounterPerMeter()
    {
        using var meter1 = new Meter(nameof(TryAcquireWithMeter_SharesOneCounterPerMeter) + "1");
        using var meter2 = new Meter(nameof(TryAcquireWithMeter_SharesOneCounterPerMeter) + "2");
        using var listener1 = new DroppedCounterListener(meter1);
        using var listener2 = new DroppedCounterListener(meter2);
        var first = Create(1, 1000, 1, new FakeClock(), "First");
        var second = Create(1, 1000, 1, new FakeClock(), "Second");

        Assert.True(first.TryAcquire(meter1));
        Assert.False(first.TryAcquire(meter1));
        Assert.False(first.TryAcquire(meter2));
        Assert.False(first.TryAcquire(meter1));
        Assert.True(second.TryAcquire(meter1));
        Assert.False(second.TryAcquire(meter1));

        Assert.Single(listener1.Instruments);
        Assert.Single(listener2.Instruments);
        Assert.Equal(new string?[] { "First", "First", "Second" }, listener1.Measurements.Select(m => m.EventName));
        Assert.Equal(new string?[] { "First" }, listener2.Measurements.Select(m => m.EventName));
    }

    private static LoggerMessageRateLimiter Create(
        int permitLimit,
        int windowMilliseconds,
        int segmentsPerWindow,
        FakeClock clock,
        string eventName = EventName)
        => new(permitLimit, windowMilliseconds, segmentsPerWindow, eventName, clock.GetTimestamp, Frequency);

    private sealed class FakeClock
    {
        public long Now { get; set; }

        public long GetTimestamp() => Now;
    }

    private sealed class DroppedCounterListener : IDisposable
    {
        private readonly MeterListener _listener = new();

        public DroppedCounterListener(Meter meter)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, meter) && instrument.Name == DroppedCounterName)
                {
                    Instruments.Add(instrument);
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                string? eventName = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == EventNameTag)
                    {
                        eventName = tag.Value as string;
                    }
                }

                Measurements.Add((value, eventName));
            });

            _listener.Start();
        }

        public List<Instrument> Instruments { get; } = [];

        public List<(long Value, string? EventName)> Measurements { get; } = [];

        public void Dispose() => _listener.Dispose();
    }
}

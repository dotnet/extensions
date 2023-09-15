// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Testing.Test;

public static class MetricCollectorTests
{
    [Fact]
    public static void Constructor_NullAndEmptyChecks()
    {
        Assert.Throws<ArgumentNullException>(() => new MetricCollector<long>((Instrument<long>)null!));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector<long>((ObservableInstrument<long>)null!));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector<long>(new Meter(Guid.NewGuid().ToString()), null!));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector<long>(null!, "Hello"));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector<long>(null, null!, "Hello"));

        Assert.Throws<ArgumentException>(() => new MetricCollector<long>(new Meter(Guid.NewGuid().ToString()), string.Empty));
        Assert.Throws<ArgumentException>(() => new MetricCollector<long>(null, string.Empty, "Hello"));
        Assert.Throws<ArgumentException>(() => new MetricCollector<long>(null, "Hello", string.Empty));
    }

    [Fact]
    public static void Constructor_TypeChecks()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("Counter");

        Assert.Throws<InvalidOperationException>(() => new MetricCollector<Guid>(meter, "Counter"));
        Assert.Throws<InvalidOperationException>(() => new MetricCollector<Guid>(null, meter.Name, "Counter"));
    }

    [Fact]
    public static void Constructor_Meter()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName, timeProvider);

        Assert.Null(collector.Instrument);
        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);

        var counter = meter.CreateCounter<long>(CounterName);
        counter.Add(3);

        // verify the update was recorded
        Assert.Equal(counter, collector.Instrument);
        Assert.NotNull(collector.LastMeasurement);

        // verify measurement info is correct
        Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(3, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(now, collector.LastMeasurement.Timestamp);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        counter.Add(2);

        // verify measurement info is correct
        Assert.Equal(2, collector.GetMeasurementSnapshot().Count);
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(2, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(timeProvider.GetUtcNow(), collector.LastMeasurement.Timestamp);

        collector.Clear();
        Assert.Equal(counter, collector.Instrument);
        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);
        Assert.Null(collector.LastMeasurement);
    }

    [Fact]
    public static void Constructor_Instrument()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>(CounterName);
        using var collector = new MetricCollector<long>(counter, timeProvider);

        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);

        counter.Add(3);

        // verify the update was recorded
        Assert.Equal(counter, collector.Instrument);
        Assert.NotNull(collector.LastMeasurement);

        // verify measurement info is correct
        Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(3, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(now, collector.LastMeasurement.Timestamp);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        counter.Add(2);

        // verify measurement info is correct
        Assert.Equal(2, collector.GetMeasurementSnapshot().Count);
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(2, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(timeProvider.GetUtcNow(), collector.LastMeasurement.Timestamp);

        collector.Clear();
        Assert.Equal(counter, collector.Instrument);
        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);
        Assert.Null(collector.LastMeasurement);
    }

    [Fact]
    public static void Constructor_Scope()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        var scope = new object();
        using var meter = new Meter(Guid.NewGuid().ToString(), null, null, scope);
        var counter = meter.CreateCounter<long>(CounterName);
        using var collector = new MetricCollector<long>(scope, meter.Name, counter.Name, timeProvider);
        using var collector2 = new MetricCollector<long>(new object(), meter.Name, counter.Name, timeProvider);

        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);

        counter.Add(3);

        // verify the update was recorded
        Assert.Equal(counter, collector.Instrument);
        Assert.NotNull(collector.LastMeasurement);

        // verify measurement info is correct
        Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(3, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(now, collector.LastMeasurement.Timestamp);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        counter.Add(2);

        // verify measurement info is correct
        Assert.Equal(2, collector.GetMeasurementSnapshot().Count);
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(2, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(timeProvider.GetUtcNow(), collector.LastMeasurement.Timestamp);

        collector.Clear();
        Assert.Equal(counter, collector.Instrument);
        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);
        Assert.Null(collector.LastMeasurement);

        Assert.Null(collector2.LastMeasurement);
    }

    [Fact]
    public static void Constructor_ObservableInstrument()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        int observationCount = 0;

        var counter = meter.CreateObservableCounter<long>(CounterName, () =>
        {
            if (observationCount == 0)
            {
                observationCount++;
                return 3;
            }
            else
            {
                return 2;
            }
        });

        using var collector = new MetricCollector<long>(counter, timeProvider);

        Assert.Empty(collector.GetMeasurementSnapshot());
        Assert.Null(collector.LastMeasurement);

        collector.RecordObservableInstruments();

        // verify the update was recorded
        Assert.Equal(counter, collector.Instrument);
        Assert.NotNull(collector.LastMeasurement);

        // verify measurement info is correct
        Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(3, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(now, collector.LastMeasurement.Timestamp);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        collector.RecordObservableInstruments();

        // verify measurement info is correct
        Assert.Equal(2, collector.GetMeasurementSnapshot().Count);
        Assert.Same(collector.GetMeasurementSnapshot().Last(), collector.LastMeasurement);
        Assert.Equal(2, collector.LastMeasurement.Value);
        Assert.Empty(collector.LastMeasurement.Tags);
        Assert.Equal(timeProvider.GetUtcNow(), collector.LastMeasurement.Timestamp);
    }

    [Fact]
    public static async Task Wait()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        var counter = meter.CreateCounter<long>(CounterName);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await collector.WaitForMeasurementsAsync(-1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await collector.WaitForMeasurementsAsync(0));

        Assert.Equal(0, collector.WaitersCount);

        var wait = collector.WaitForMeasurementsAsync(2);
        Assert.Equal(1, collector.WaitersCount);
        Assert.False(wait.IsCompleted);

        counter.Add(1);
        Assert.False(wait.IsCompleted);
        Assert.Equal(1, collector.WaitersCount);

        counter.Add(1);
        Assert.True(wait.IsCompleted);
        Assert.False(wait.IsFaulted);
        Assert.Equal(0, collector.WaitersCount);

        collector.Clear();
        counter.Add(1);
        wait = collector.WaitForMeasurementsAsync(1);
        Assert.True(wait.IsCompleted);
        Assert.False(wait.IsFaulted);
        Assert.Equal(0, collector.WaitersCount);
    }

    [Fact]
    public static async Task WaitWithCancellation()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        var counter = meter.CreateCounter<long>(CounterName);

        using var cts = new CancellationTokenSource();
        var wait = collector.WaitForMeasurementsAsync(1, cts.Token);

        Assert.Equal(1, collector.WaitersCount);

        cts.Cancel();

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

        Assert.Equal(0, collector.WaitersCount);
    }

    [Fact]
    public static async Task WaitWithTimeout()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        var counter = meter.CreateCounter<long>(CounterName);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await collector.WaitForMeasurementsAsync(-1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await collector.WaitForMeasurementsAsync(0));

        var wait = collector.WaitForMeasurementsAsync(2, TimeSpan.FromSeconds(1));
        Assert.False(wait.IsCompleted);
        Assert.Equal(1, collector.WaitersCount);

        counter.Add(1);
        Assert.False(wait.IsCompleted);
        Assert.Equal(1, collector.WaitersCount);

        collector.Clear();
        wait = collector.WaitForMeasurementsAsync(1, TimeSpan.FromSeconds(1));
        Assert.Equal(2, collector.WaitersCount);
        counter.Add(1);
        Assert.Equal(1, collector.WaitersCount);

        // Task should be complete. Error if not complete after delay.
        await wait.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact]
    public static async Task WaitWithTimeout_CanceledFromTimeout()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName);
        var counter = meter.CreateCounter<long>(CounterName);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => collector.WaitForMeasurementsAsync(1, TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public static async Task Dispose()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        var collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        var counter = meter.CreateCounter<long>(CounterName);

        var wait = collector.WaitForMeasurementsAsync(2, TimeSpan.FromSeconds(1));

        collector.Dispose();
        collector.Dispose();    // second call is a nop

        Assert.Throws<ObjectDisposedException>(() => collector.GetMeasurementSnapshot());
        Assert.Throws<ObjectDisposedException>(() => collector.Clear());
        Assert.Throws<ObjectDisposedException>(() => collector.LastMeasurement);
        Assert.Throws<ObjectDisposedException>(() => collector.RecordObservableInstruments());

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await collector.WaitForMeasurementsAsync(1));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await collector.WaitForMeasurementsAsync(1, TimeSpan.FromSeconds(1)));

        // HACK: there seems to be something executing asynchronously which takes a while to finish. This needs to be tracked on
        //       I'm adding the sleep here to keep the test from being flaky.
        Thread.Sleep(2000);

        Assert.True(wait.IsCompleted);
        Assert.True(wait.IsFaulted);

        collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        collector.Dispose();
        counter.Add(1);
        Assert.Throws<ObjectDisposedException>(() => collector.Clear());
    }

    [Fact]
    public static void Snapshot()
    {
        const string CounterName = "MyCounter";

        var now = DateTimeOffset.Now;

        var timeProvider = new FakeTimeProvider(now);
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var collector = new MetricCollector<long>(meter, CounterName, timeProvider);
        var counter = meter.CreateCounter<long>(CounterName);

        counter.Add(1);
        counter.Add(2);
        counter.Add(3);

        var snap = collector.GetMeasurementSnapshot();
        Assert.Equal(3, snap.Count);
        Assert.Equal(3, collector.GetMeasurementSnapshot().Count);

        Assert.Equal(1, snap[0].Value);
        Assert.Empty(snap[0].Tags);
        Assert.Equal(now, snap[0].Timestamp);

        Assert.Equal(2, snap[1].Value);
        Assert.Empty(snap[1].Tags);
        Assert.Equal(now, snap[1].Timestamp);

        Assert.Equal(3, snap[2].Value);
        Assert.Empty(snap[2].Tags);
        Assert.Equal(now, snap[2].Timestamp);

        snap = collector.GetMeasurementSnapshot(true);
        Assert.Equal(3, snap.Count);
        Assert.Equal(0, collector.GetMeasurementSnapshot().Count);
    }
}

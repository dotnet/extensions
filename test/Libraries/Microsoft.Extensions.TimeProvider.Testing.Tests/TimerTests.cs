// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Time.Testing.Test;

public class TimerTests
{
    private void EmptyTimerTarget(object? o)
    {
        // no-op for timer callbacks
    }

    [Fact]
    public void TimerNonPeriodicPeriodZero()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        using var timer = timeProvider.CreateTimer(_ => { counter++; }, null, TimeSpan.FromMilliseconds(10), TimeSpan.Zero);

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        var value2 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1000));

        var value3 = counter;

        Assert.Equal(0, value1);
        Assert.Equal(1, value2);
        Assert.Equal(1, value3);
    }

    [Fact]
    public void TimerNonPeriodicPeriodInfinite()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        using var timer = timeProvider.CreateTimer(_ => { counter++; }, null, TimeSpan.FromMilliseconds(10), Timeout.InfiniteTimeSpan);

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        var value2 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1000));

        var value3 = counter;

        Assert.Equal(0, value1);
        Assert.Equal(1, value2);
        Assert.Equal(1, value3);
    }

    [Fact]
    public void TimerStartsImmediately()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        using var timer = timeProvider.CreateTimer(_ => { counter++; }, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        var value2 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        var value3 = counter;

        Assert.Equal(1, value1);
        Assert.Equal(1, value2);
        Assert.Equal(1, value3);
    }

    [Fact]
    public void NoDueTime_TimerDoesntStart()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        var timer = timeProvider.CreateTimer(_ => { counter++; }, null, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(10));

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var value2 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(50));

        var value3 = counter;

        Assert.Equal(0, value1);
        Assert.Equal(0, value2);
        Assert.Equal(0, value3);
    }

    [Fact]
    public void TimerTriggersPeriodically()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        var timer = timeProvider.CreateTimer(_ => { counter++; }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var value2 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        var value3 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        var value4 = counter;

        Assert.Equal(1, value1);
        Assert.Equal(1, value2);
        Assert.Equal(2, value3);
        Assert.Equal(3, value4);
    }

    [Fact]
    public async Task TaskDelayWithFakeTimeProviderAdvanced()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));

        var task = fakeTimeProvider.Delay(TimeSpan.FromMilliseconds(10000), cancellationTokenSource.Token);

        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(10000));

        await task;

        Assert.False(cancellationTokenSource.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task TaskDelayWithFakeTimeProviderStopped()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await fakeTimeProvider.Delay(
                TimeSpan.FromMilliseconds(10000),
                cancellationTokenSource.Token)
            .ConfigureAwait(false);
        });
    }

    [Fact]
    public void TimerChangeDueTimeOutOfRangeThrows()
    {
        using var t = new Timer(new FakeTimeProvider(), new TimerCallback(EmptyTimerTarget), null);
        _ = t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(-2), TimeSpan.FromMilliseconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(-2), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(0xFFFFFFFFL), TimeSpan.FromMilliseconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(0xFFFFFFFFL), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void TimerChangePeriodOutOfRangeThrows()
    {
        using var t = new Timer(new FakeTimeProvider(), new TimerCallback(EmptyTimerTarget), null);
        _ = t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(0xFFFFFFFFL)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(0xFFFFFFFFL)));
    }

    [Fact]
    public void Timer_Change_AfterDispose_Test()
    {
        var t = new Timer(new FakeTimeProvider(), new TimerCallback(EmptyTimerTarget), null);
        _ = t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        Assert.True(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        t.Dispose();
        Assert.False(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public async Task Timer_Change_AfterDisposeAsync_Test()
    {
        var t = new Timer(new FakeTimeProvider(), new TimerCallback(EmptyTimerTarget), null);
        _ = t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        Assert.True(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        await t.DisposeAsync();
        Assert.False(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public void WaiterRemovedAfterDispose()
    {
        var timer1Counter = 0;
        var timer2Counter = 0;

        var timeProvider = new FakeTimeProvider();
        var waitersCountStart = timeProvider.Waiters.Count;

        var timer1 = timeProvider.CreateTimer(_ => timer1Counter++, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
        var timer2 = timeProvider.CreateTimer(_ => timer2Counter++, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        var waitersCountDuring = timeProvider.Waiters.Count;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        timer1.Dispose();

        var waitersCountAfterDispose = timeProvider.Waiters.Count;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var waitersCountOnFinish = timeProvider.Waiters.Count;

        Assert.Equal(0, waitersCountStart);
        Assert.Equal(2, waitersCountDuring);
        Assert.Equal(1, waitersCountAfterDispose);
        Assert.Equal(1, waitersCountOnFinish);
        Assert.Equal(1, timer1Counter);
        Assert.Equal(2, timer2Counter);
    }

#if RELEASE // In Release only since this might not work if the timer reference being tracked by the debugger
    [Fact(Skip = "Flaky on .NET Framework")]
    public void WaiterRemovedWhenCollectedWithoutDispose()
    {
        var timer1Counter = 0;
        var timer2Counter = 0;

        var timeProvider = new FakeTimeProvider();
        var waitersCountStart = timeProvider.Waiters.Count;

        var timer1 = timeProvider.CreateTimer(_ => timer1Counter++, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
        var timer2 = timeProvider.CreateTimer(_ => timer2Counter++, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        var waitersCountDuring = timeProvider.Waiters.Count;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        // Force the finalizer on timer1 to ensure Dispose is releasing the waiter object
        // even when a Timer is not disposed
        timer1 = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var waitersCountAfter = timeProvider.Waiters.Count;

        Assert.Equal(0, waitersCountStart);
        Assert.Equal(2, waitersCountDuring);
        Assert.Equal(1, timer1Counter);
        Assert.Equal(2, timer2Counter);
        Assert.Equal(1, waitersCountAfter);
    }
#endif

    [Fact]
    public void UtcNowUpdatedBeforeTimerCallback()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var callbackTime = DateTimeOffset.MinValue;
        using var timer = timeProvider.CreateTimer(_ => { callbackTime = timeProvider.GetUtcNow(); }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));

        var value1 = callbackTime;

        timeProvider.SetUtcNow(timeProvider.Start + TimeSpan.FromMilliseconds(20));

        var value2 = callbackTime;

        timeProvider.SetUtcNow(timeProvider.Start + TimeSpan.FromMilliseconds(1000));

        var value3 = callbackTime;

        Assert.Equal(timeProvider.Start, value1);
        Assert.Equal(timeProvider.Start + TimeSpan.FromMilliseconds(20), value2);
        Assert.Equal(timeProvider.Start + TimeSpan.FromMilliseconds(1000), value3);
    }

    [Fact]
    public void LongPausesTriggerMultipleCallbacks()
    {
        var callbackTimes = new List<DateTimeOffset>();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var period = TimeSpan.FromMilliseconds(10);
        var timer = timeProvider.CreateTimer(_ => { callbackTimes.Add(timeProvider.GetUtcNow()); }, null, TimeSpan.Zero, period);

        var value1 = callbackTimes.ToArray();

        timeProvider.Advance(period + period + period);

        var value2 = callbackTimes.ToArray();

        Assert.Equal(new[] { timeProvider.Start }, value1);
        Assert.Equal(new[]
        {
            timeProvider.Start,
            timeProvider.Start + period + period + period,
            timeProvider.Start + period + period + period,
            timeProvider.Start + period + period + period,
        },
        value2);
    }

    [Fact]
    public void MultipleTimersCallbackInvokedInScheduledOrder()
    {
        var callbacks = new List<(int timerId, TimeSpan callbackTime)>();
        var timeProvider = new FakeTimeProvider();
        var startTime = timeProvider.GetTimestamp();
        using var timer1 = timeProvider.CreateTimer(_ => callbacks.Add((1, timeProvider.GetElapsedTime(startTime))), null, TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(3));
        using var timer2 = timeProvider.CreateTimer(_ => callbacks.Add((2, timeProvider.GetElapsedTime(startTime))), null, TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(3));
        using var timer3 = timeProvider.CreateTimer(_ => callbacks.Add((3, timeProvider.GetElapsedTime(startTime))), null, TimeSpan.FromMilliseconds(6), TimeSpan.FromMilliseconds(5));

        timeProvider.Advance(TimeSpan.FromMilliseconds(3));
        timeProvider.Advance(TimeSpan.FromMilliseconds(3));
        timeProvider.Advance(TimeSpan.FromMilliseconds(3));
        timeProvider.Advance(TimeSpan.FromMilliseconds(2));

        Assert.Equal(new[]
        {
            (1, TimeSpan.FromMilliseconds(3)),
            (2, TimeSpan.FromMilliseconds(3)),
            (3, TimeSpan.FromMilliseconds(6)),
            (1, TimeSpan.FromMilliseconds(6)),
            (2, TimeSpan.FromMilliseconds(6)),
            (1, TimeSpan.FromMilliseconds(9)),
            (2, TimeSpan.FromMilliseconds(9)),
            (3, TimeSpan.FromMilliseconds(11)),
        },
        callbacks);
    }

    [Fact]
    public void OutOfOrderWakeTimes()
    {
        const int MaxDueTime = 10;
        const int TotalTimers = 128;

        var timeProvider = new FakeTimeProvider();
        var triggers = new bool[TotalTimers];
        var dueTimes = new int[TotalTimers];
        var random = new Random();
        var timers = new List<ITimer>();

        for (int i = 0; i < triggers.Length; i++)
        {
            dueTimes[i] = random.Next(MaxDueTime);
            timers.Add(timeProvider.CreateTimer(index => triggers[(int)index!] = true, i, TimeSpan.FromSeconds(dueTimes[i]), TimeSpan.Zero));
            timeProvider.Advance(TimeSpan.FromTicks(1));
        }

        for (int i = 0; i < MaxDueTime + 1; i += 2)
        {
            for (int j = 0; j < TotalTimers; j++)
            {
                if (dueTimes[j] <= i)
                {
                    Assert.True(triggers[j]);
                }
                else
                {
                    Assert.False(triggers[j]);
                }
            }

            timeProvider.Advance(TimeSpan.FromSeconds(2));
        }

        foreach (var timer in timers)
        {
            timer.Dispose();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Time.Testing.Test;

public class FakeTimeProviderTimerTests
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
    public void LongPausesTriggerSingleCallback()
    {
        var counter = 0;
        var timeProvider = new FakeTimeProvider();
        var timer = timeProvider.CreateTimer(_ => { counter++; }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));

        var value1 = counter;

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        var value2 = counter;

        Assert.Equal(1, value1);
        Assert.Equal(2, value2);
    }

    [Fact]
    public async Task TaskDelayWithFakeTimeProviderAdvanced()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));

        var task = fakeTimeProvider.Delay(TimeSpan.FromMilliseconds(10000), cancellationTokenSource.Token).ConfigureAwait(false);

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
        using var t = new FakeTimeProviderTimer(new FakeTimeProvider(), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), new TimerCallback(EmptyTimerTarget), null);

        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(-2), TimeSpan.FromMilliseconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(-2), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(0xFFFFFFFFL), TimeSpan.FromMilliseconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(0xFFFFFFFFL), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void TimerChangePeriodOutOfRangeThrows()
    {
        using var t = new FakeTimeProviderTimer(new FakeTimeProvider(), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), new TimerCallback(EmptyTimerTarget), null);

        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(0xFFFFFFFFL)));
        Assert.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(0xFFFFFFFFL)));
    }

    [Fact]
    public void Timer_Change_AfterDispose_Test()
    {
        var t = new FakeTimeProviderTimer(new FakeTimeProvider(), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), new TimerCallback(EmptyTimerTarget), null);

        Assert.True(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        t.Dispose();
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

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var waitersCountAfter = timeProvider.Waiters.Count;

        Assert.Equal(0, waitersCountStart);
        Assert.Equal(2, waitersCountDuring);
        Assert.Equal(1, timer1Counter);
        Assert.Equal(2, timer2Counter);
        Assert.Equal(1, waitersCountAfter);
    }

#if RELEASE // In Release only since this might not work if the timer reference being tracked by the debugger
    [Fact]
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
}

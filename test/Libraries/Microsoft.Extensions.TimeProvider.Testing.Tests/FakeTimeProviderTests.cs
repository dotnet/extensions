// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Time.Testing.Test;

public class FakeTimeProviderTests
{
    [Fact]
    public void Constructor_DefaultInitialization_SetsExpectedValues()
    {
        var timeProvider = new FakeTimeProvider();

        var now = timeProvider.GetUtcNow();
        var timestamp = timeProvider.GetTimestamp();
        var frequency = timeProvider.TimestampFrequency;

        Assert.Equal(2000, now.Year);
        Assert.Equal(1, now.Month);
        Assert.Equal(1, now.Day);
        Assert.Equal(0, now.Hour);
        Assert.Equal(0, now.Minute);
        Assert.Equal(0, now.Second);
        Assert.Equal(0, now.Millisecond);
        Assert.Equal(TimeSpan.Zero, now.Offset);
        Assert.Equal(10_000_000, frequency);
        Assert.Equal(TimeSpan.Zero, timeProvider.AutoAdvanceAmount);

        var timestamp2 = timeProvider.GetTimestamp();
        var frequency2 = timeProvider.TimestampFrequency;
        var now2 = timeProvider.GetUtcNow();

        Assert.Equal(now, now2);
        Assert.Equal(frequency, frequency2);
        Assert.Equal(timestamp, timestamp2);
    }

    [Fact]
    public void GetTimestamp_WithoutAutoAdvance_DoesNotAdvance()
    {
        var nowOffset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(nowOffset);

        var timestamp1 = timeProvider.GetTimestamp();
        var timestamp2 = timeProvider.GetTimestamp();

        Assert.Equal(nowOffset, new DateTimeOffset(timestamp1, TimeSpan.Zero));
        Assert.Equal(nowOffset, new DateTimeOffset(timestamp2, TimeSpan.Zero));
    }

    [Fact]
    public void GetTimestamp_WithAutoAdvance_AdvancesProperly()
    {
        var msToElapse = 100;
        var nowOffset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(nowOffset)
        {
            AutoAdvanceAmount = TimeSpan.FromMilliseconds(msToElapse)
        };

        var timestamp1 = timeProvider.GetTimestamp();
        var timestamp2 = timeProvider.GetTimestamp();

        Assert.Equal(nowOffset, new DateTimeOffset(timestamp1, TimeSpan.Zero));
        Assert.Equal(nowOffset.AddMilliseconds(msToElapse), new DateTimeOffset(timestamp2, TimeSpan.Zero));
    }

    [Fact]
    public void GetElapsedTime_CallsGetTimestamp()
    {
        var timeProvider = new FakeTimeProvider();
        var st = timeProvider.GetTimestamp();
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        var t = timeProvider.GetElapsedTime(st);

        Assert.Equal(TimeSpan.FromSeconds(1), t);
    }

    [Fact]
    public void GetElapsedTime_WithAutoAdvance_AdvancesProperly()
    {
        var timeProvider = new FakeTimeProvider()
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1)
        };

        var st = timeProvider.GetTimestamp();
        var elapsedTime = timeProvider.GetElapsedTime(st);
        var elapsedTime2 = timeProvider.GetElapsedTime(st);

        Assert.Equal(TimeSpan.FromSeconds(1), elapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(2), elapsedTime2);
    }

    [Fact]
    public void Constructor_InitializesWithCustomDateTimeOffset_AdvancesCorrectly()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));

        timeProvider.Advance(TimeSpan.FromMilliseconds(8));
        var pnow = timeProvider.GetTimestamp();
        var frequency = timeProvider.TimestampFrequency;
        var now = timeProvider.GetUtcNow();

        Assert.Equal(2001, now.Year);
        Assert.Equal(2, now.Month);
        Assert.Equal(3, now.Day);
        Assert.Equal(4, now.Hour);
        Assert.Equal(5, now.Minute);
        Assert.Equal(6, now.Second);
        Assert.Equal(TimeSpan.Zero, now.Offset);
        Assert.Equal(8, now.Millisecond);
        Assert.Equal(10_000_000, frequency);
        Assert.Equal(TimeSpan.Zero, timeProvider.AutoAdvanceAmount);

        timeProvider.Advance(TimeSpan.FromMilliseconds(8));
        var pnow2 = timeProvider.GetTimestamp();
        var frequency2 = timeProvider.TimestampFrequency;
        now = timeProvider.GetUtcNow();

        Assert.Equal(2001, now.Year);
        Assert.Equal(2, now.Month);
        Assert.Equal(3, now.Day);
        Assert.Equal(4, now.Hour);
        Assert.Equal(5, now.Minute);
        Assert.Equal(6, now.Second);
        Assert.Equal(16, now.Millisecond);
        Assert.Equal(frequency, frequency2);
        Assert.True(pnow2 > pnow);
    }

    [Fact]
    public void LocalTimeZone_Default_IsUtc()
    {
        var timeProvider = new FakeTimeProvider();
        var localTimeZone = timeProvider.LocalTimeZone;

        Assert.Equal(TimeZoneInfo.Utc, localTimeZone);
    }

    [Fact]
    public void SetLocalTimeZone_CustomTimeZone_SetsNewTimeZone()
    {
        var timeProvider = new FakeTimeProvider();

        var localTimeZone = timeProvider.LocalTimeZone;
        Assert.Equal(TimeZoneInfo.Utc, localTimeZone);

        var tz = TimeZoneInfo.CreateCustomTimeZone("DUMMY", TimeSpan.FromHours(2), null, null);
        timeProvider.SetLocalTimeZone(tz);
        Assert.Equal(timeProvider.LocalTimeZone, tz);
    }

    [Fact]
    public void SetUtcNow_Forward_AdvancesByProperAmount()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));

        var initialTimeUtcNow = timeProvider.GetUtcNow();
        var initialTimestamp = timeProvider.GetTimestamp();

        timeProvider.SetUtcNow(timeProvider.GetUtcNow().AddMilliseconds(1234));

        var finalTimeUtcNow = timeProvider.GetUtcNow();
        var finalTimeTimestamp = timeProvider.GetTimestamp();

        var utcDelta = finalTimeUtcNow - initialTimeUtcNow;
        var perfDelta = finalTimeTimestamp - initialTimestamp;
        var elapsedTime = timeProvider.GetElapsedTime(initialTimestamp, finalTimeTimestamp);

        Assert.Equal(1, utcDelta.Seconds);
        Assert.Equal(234, utcDelta.Milliseconds);
        Assert.Equal(1234D, utcDelta.TotalMilliseconds);
        Assert.Equal(1.234D, (double)perfDelta / timeProvider.TimestampFrequency, 3);
        Assert.Equal(1234, elapsedTime.TotalMilliseconds);
    }

    [Fact]
    public void Advance_Forward_AdvancesByProperAmount()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));

        var initialTimeUtcNow = timeProvider.GetUtcNow();
        var initialTimestamp = timeProvider.GetTimestamp();

        timeProvider.Advance(TimeSpan.FromMilliseconds(1234));

        var finalTimeUtcNow = timeProvider.GetUtcNow();
        var finalTimeTimestamp = timeProvider.GetTimestamp();

        var utcDelta = finalTimeUtcNow - initialTimeUtcNow;
        var perfDelta = finalTimeTimestamp - initialTimestamp;
        var elapsedTime = timeProvider.GetElapsedTime(initialTimestamp, finalTimeTimestamp);

        Assert.Equal(1, utcDelta.Seconds);
        Assert.Equal(234, utcDelta.Milliseconds);
        Assert.Equal(1234D, utcDelta.TotalMilliseconds);
        Assert.Equal(1.234D, (double)perfDelta / timeProvider.TimestampFrequency, 3);
        Assert.Equal(1234, elapsedTime.TotalMilliseconds);
    }

    [Fact]
    public void Advance_Backwards_ThrowsArgumentOutOfRangeException()
    {
        var timeProvider = new FakeTimeProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => timeProvider.Advance(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void SetUtcNow_Backwards_ThrowsArgumentOutOfRangeException()
    {
        var timeProvider = new FakeTimeProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => timeProvider.SetUtcNow(timeProvider.GetUtcNow() - TimeSpan.FromTicks(1)));
    }

    [Fact]
    public void TimerCallback_AdjustTimeForward_Works()
    {
        var tp = new FakeTimeProvider();

        var t1Tick = 0;
        var t1 = tp.CreateTimer(_ =>
        {
            t1Tick++;
        }, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);

        tp.AdjustTime(tp.GetUtcNow() + TimeSpan.FromMilliseconds(999));
        Assert.Equal(0, t1Tick);

        tp.AdjustTime(tp.GetUtcNow() + TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, t1Tick);

        tp.AdjustTime(tp.GetUtcNow() + TimeSpan.FromSeconds(10));
        Assert.Equal(0, t1Tick);

        tp.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Equal(0, t1Tick);

        tp.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, t1Tick);
    }

    [Fact]
    public void TimerCallback_AdjustTimeBackwards_Works()
    {
        var tp = new FakeTimeProvider();

        var t1Tick = 0;
        var t1 = tp.CreateTimer(_ =>
        {
            t1Tick++;
        }, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);

        tp.AdjustTime(tp.GetUtcNow() - TimeSpan.FromMilliseconds(999));
        Assert.Equal(0, t1Tick);

        tp.AdjustTime(tp.GetUtcNow() - TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, t1Tick);

        tp.AdjustTime(tp.GetUtcNow() - TimeSpan.FromSeconds(10));
        Assert.Equal(0, t1Tick);

        tp.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Equal(0, t1Tick);

        tp.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, t1Tick);
    }

    [Fact]
    public void ToString_SetDateTimeOffset_ReturnsProperFormat()
    {
        var dto = new DateTimeOffset(new DateTime(2022, 1, 2, 3, 4, 5, 6), TimeSpan.Zero);

        var timeProvider = new FakeTimeProvider(dto);
        Assert.Equal("2022-01-02T03:04:05.006", timeProvider.ToString());
    }

    private readonly TimeSpan _infiniteTimeout = TimeSpan.FromMilliseconds(-1);

    [Fact]
    public async Task Delay_ZeroDelay_CompletesSuccessfully()
    {
        var timeProvider = new FakeTimeProvider();
        var t = timeProvider.Delay(TimeSpan.Zero, CancellationToken.None);
        await t;

        Assert.True(t.IsCompleted && !t.IsFaulted);
    }

    [Fact]
    public async Task Delay_Awaited_CompletesSuccessfully()
    {
        var timeProvider = new FakeTimeProvider();

        var delay = timeProvider.Delay(TimeSpan.FromMilliseconds(1), CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await delay;

        Assert.True(delay.IsCompleted);
        Assert.False(delay.IsFaulted);
        Assert.False(delay.IsCanceled);
    }

    [Fact]
    public async Task Delay_TokenCancelled_ThrowsTaskCanceledException()
    {
        var timeProvider = new FakeTimeProvider();

        using var cs = new CancellationTokenSource();
        var delay = timeProvider.Delay(_infiniteTimeout, cs.Token);
        Assert.False(delay.IsCompleted);

        cs.Cancel();

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await delay);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }

    [Fact]
    public async Task Delay_WhenTimeAdvanced_CompletesWithoutCancellation()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));

        var task = fakeTimeProvider.Delay(TimeSpan.FromMilliseconds(10000), cancellationTokenSource.Token);

        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(10000));

        await task;

        Assert.False(cancellationTokenSource.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task Advance_CancelledToken_ThrowsTaskCanceledException()
    {
        var timeProvider = new FakeTimeProvider();

        using var cts = timeProvider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(1));
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        await Assert.ThrowsAsync<TaskCanceledException>(() => timeProvider.Delay(TimeSpan.FromTicks(1), cts.Token));
    }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
    [Fact]
    public async Task WaitAsync_NegativeTimeout_Throws()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new TaskCompletionSource<bool>();

#if NET8_0_OR_GREATER
        await Assert.ThrowsAsync<TimeoutException>(() => source.Task.WaitAsync(TimeSpan.FromTicks(-1), timeProvider, CancellationToken.None));
#else
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => source.Task.WaitAsync(TimeSpan.FromTicks(-1), timeProvider, CancellationToken.None));
#endif
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => source.Task.WaitAsync(TimeSpan.FromMilliseconds(-2), timeProvider, CancellationToken.None));
    }

    [Fact]
    public async Task WaitAsync_ValidTimeout_CompletesSuccessfully()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new TaskCompletionSource<bool>();

        var t = source.Task.WaitAsync(TimeSpan.FromSeconds(100000), timeProvider, CancellationToken.None);
        while (!t.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
            await Task.Delay(1);
            _ = source.TrySetResult(true);
        }

        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
    }
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

    [Fact]
    public async Task WaitAsync_InfiniteTimeout_CompletesSuccessfully()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new TaskCompletionSource<bool>();

        var t = source.Task.WaitAsync(TimeSpan.FromMilliseconds(-1), timeProvider, CancellationToken.None);
        while (!t.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
            await Task.Delay(1);
            _ = source.TrySetResult(true);
        }

        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
    }

    [Fact]
    public async Task WaitAsync_Timeout_ResultsInFaultedTask()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new TaskCompletionSource<bool>();

        var t = source.Task.WaitAsync(TimeSpan.FromMilliseconds(1), timeProvider, CancellationToken.None);
        while (!t.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
            await Task.Delay(1);
        }

        Assert.True(t.IsCompleted);
        Assert.True(t.IsFaulted);
        Assert.False(t.IsCanceled);
    }

    [Fact]
    public async Task WaitAsync_CancelledToken_ThrowsTaskCanceledException()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource();

        var t = source.Task.WaitAsync(_infiniteTimeout, timeProvider, cts.Token);
        cts.Cancel();

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await Assert.ThrowsAsync<TaskCanceledException>(() => t);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }

    [Fact]
    public void GetUtcNow_AutoAdvanceSpecified_AutoAdvancesBySpecifiedAmount()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow)
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1)
        };

        var first = timeProvider.GetUtcNow();
        var second = timeProvider.GetUtcNow();
        var third = timeProvider.GetUtcNow();

        Assert.Equal(timeProvider.Start, first);
        Assert.Equal(timeProvider.Start + TimeSpan.FromSeconds(1), second);
        Assert.Equal(timeProvider.Start + TimeSpan.FromSeconds(2), third);
    }

    [Fact]
    public void ToString_NoAutoAdvanceSpecified_DoesNotAutoAdvance()
    {
        var timeProvider = new FakeTimeProvider();

        _ = timeProvider.ToString();

        Assert.Equal(timeProvider.Start, timeProvider.GetUtcNow());
    }

    [Fact]
    public void ToString_AutoAdvanceSpecified_AutoAdvancesBySpecifiedAmount()
    {
        var timeProvider = new FakeTimeProvider
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1)
        };

        _ = timeProvider.ToString();

        timeProvider.AutoAdvanceAmount = TimeSpan.Zero;
        Assert.Equal(timeProvider.Start, timeProvider.GetUtcNow());
    }

    [Fact]
    public void Advance_TimeInCallback_PreventsInfiniteLoop()
    {
        var oneSecond = TimeSpan.FromSeconds(1);
        var timeProvider = new FakeTimeProvider();

        var timer = timeProvider.CreateTimer(_ =>
        {
            // Advance the time with exactly the same amount as the period of the timer. This could lead to an
            // infinite loop where this callback repeatedly gets invoked. A correct implementation however
            // will adjust the timer's wake time so this won't be a problem.
            timeProvider.Advance(oneSecond);
        }, null, TimeSpan.Zero, oneSecond);

        Assert.True(true, "Yay, we didn't enter an infinite loop!");
    }

    [Fact]
    public void GetUtcNow_ResetGateUnderLock_PreventsContextSwitchingIssuesWithTimerCallback()
    {
        // Arrange
        var provider = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromSeconds(2) };
        var calls = new List<object?>();
        using var timer = provider.CreateTimer(calls.Add, "timer-1", TimeSpan.FromSeconds(3), TimeSpan.Zero);
        var th = new Thread(() => provider.GetUtcNow());
        provider.GateOpening += (_, _) =>
        {
            if (!th.IsAlive)
            {
                th.Start();
            }

            // use a timeout to prevent deadlock
            th.Join(TimeSpan.FromMilliseconds(200));
        };

        // Act
        provider.GetUtcNow();
        th.Join();

        // Assert
        Assert.Single(calls);
    }

    [Fact]
    public void Advance_PollyRetryWithConfigureAwaitFalse_ProcessesCorrectly()
    {
        // Arrange
        SynchronizationContext.SetSynchronizationContext(null);
        var retries = 42;
        var tries = 0;
        var taskDelay = 0.5;
        var delay = 1;
        var provider = new FakeTimeProvider();

        async Task<int> simulatedPollyRetry()
        {
            while (true)
            {
                try
                {
                    // simulate task that takes some time to complete
                    await provider.Delay(TimeSpan.FromSeconds(taskDelay));
                    tries++;

                    if (tries <= retries)
                    {
                        // the task failed, trigger retry
                        throw new InvalidOperationException();
                    }

                    return tries;
                }
                catch (InvalidOperationException)
                {
                    // ConfigureAwait(false) is required to validate test properly
                    await provider.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
                }
            }
        }

        // Act
        var result = simulatedPollyRetry();

        for (int i = 0; i < retries; i++)
        {
            // advance time for simulated task delay
            provider.Advance(TimeSpan.FromSeconds(taskDelay));

            // advance time for retry delay
            provider.Advance(TimeSpan.FromSeconds(delay));
        }

        // Assert
        Assert.False(result.IsCompleted);
        Assert.Equal(retries, tries);
    }
}

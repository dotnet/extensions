// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Time.Testing;

/// <summary>
/// Represents a synthetic time provider that can be used to enable deterministic behavior in tests.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    internal readonly HashSet<Waiter> Waiters = [];
    private DateTimeOffset _now = new(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
    private TimeZoneInfo _localTimeZone = TimeZoneInfo.Utc;
    private volatile int _wakeWaitersGate;
    private TimeSpan _autoAdvanceAmount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeTimeProvider"/> class.
    /// </summary>
    /// <remarks>
    /// This creates a provider whose time is initially set to midnight January 1st 2000.
    /// The provider is set to not automatically advance time each time it is read.
    /// </remarks>
    public FakeTimeProvider()
    {
        Start = _now;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeTimeProvider"/> class.
    /// </summary>
    /// <param name="startDateTime">The initial time and date reported by the provider.</param>
    /// <remarks>
    /// The provider is set to not automatically advance time each time it is read.
    /// </remarks>
    public FakeTimeProvider(DateTimeOffset startDateTime)
    {
        _ = Throw.IfLessThan(startDateTime.Ticks, 0);

        _now = startDateTime;
        Start = _now;
    }

    /// <summary>
    /// Gets the starting date and time for this provider.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets or sets the amount of time by which time advances whenever the clock is read.
    /// </summary>
    /// <remarks>
    /// This defaults to <see cref="TimeSpan.Zero"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">if the time value is set to less than <see cref="TimeSpan.Zero"/>.</exception>
    public TimeSpan AutoAdvanceAmount
    {
        get => _autoAdvanceAmount;
        set
        {
            _ = Throw.IfLessThan(value.Ticks, 0);
            _autoAdvanceAmount = value;
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        DateTimeOffset result;

        lock (Waiters)
        {
            result = _now;
            _now += _autoAdvanceAmount;
        }

        WakeWaiters();
        return result;
    }

    /// <summary>
    /// Sets the date and time in the UTC time zone.
    /// </summary>
    /// <param name="value">The date and time in the UTC time zone.</param>
    /// <exception cref="ArgumentOutOfRangeException">if the supplied time value is before the current time.</exception>
    public void SetUtcNow(DateTimeOffset value)
    {
        lock (Waiters)
        {
            if (value < _now)
            {
                Throw.ArgumentOutOfRangeException(nameof(value), $"Cannot go back in time. Current time is {_now}.");
            }

            _now = value;
        }

        WakeWaiters();
    }

    /// <summary>
    /// Advances time by a specific amount.
    /// </summary>
    /// <param name="delta">The amount of time to advance the clock by.</param>
    /// <remarks>
    /// Advancing time affects the timers created from this provider, and all other operations that are directly or
    /// indirectly using this provider as a time source. Whereas when using <see cref="TimeProvider.System"/>, time
    /// marches forward automatically in hardware, for the fake time provider the application is responsible for
    /// doing this explicitly by calling this method.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">if the time value is less than <see cref="TimeSpan.Zero"/>.</exception>
    public void Advance(TimeSpan delta)
    {
        _ = Throw.IfLessThan(delta.Ticks, 0);

        lock (Waiters)
        {
            _now += delta;
        }

        WakeWaiters();

#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test

        // Pause the current thread briefly to give any pending timers a chance to execute.
        // This pause does not alter the time reported by the provider.
        // While this may slightly increase the duration of tests, it ensures more predictable behavior.
        Thread.Sleep(1);

#pragma warning restore EA0002 // Use 'System.TimeProvider' to make the code easier to test
    }

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        // Notionally we're multiplying by frequency and dividing by ticks per second,
        // which are the same value for us. Don't actually do the math as the full
        // precision of ticks (a long) cannot be represented in a double during division.
        // For test stability we want a reproducible result.
        //
        // The same issue could occur converting back, in GetElapsedTime(). Unfortunately
        // that isn't virtual so we can't do the same trick. However, if tests advance
        // the clock in multiples of 1ms or so this loss of precision will not be visible.
        Debug.Assert(TimestampFrequency == TimeSpan.TicksPerSecond, "Assuming frequency equals ticks per second");
        return _now.Ticks;
    }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => _localTimeZone;

    /// <summary>
    /// Sets the local time zone.
    /// </summary>
    /// <param name="localTimeZone">The local time zone.</param>
    public void SetLocalTimeZone(TimeZoneInfo localTimeZone) => _localTimeZone = Throw.IfNull(localTimeZone);

    /// <summary>
    /// Gets the amount by which the value from <see cref="GetTimestamp"/> increments per second.
    /// </summary>
    /// <remarks>
    /// This is fixed to the value of <see cref="TimeSpan.TicksPerSecond"/>.
    /// </remarks>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <summary>
    /// Returns a string representation this provider's idea of current time.
    /// </summary>
    /// <returns>A string representing the provider's current time.</returns>
    public override string ToString() => _now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Timer(this, Throw.IfNull(callback), state);
        _ = timer.Change(dueTime, period);
        return timer;
    }

    internal void RemoveWaiter(Waiter waiter)
    {
        lock (Waiters)
        {
            _ = Waiters.Remove(waiter);
        }
    }

    internal void AddWaiter(Waiter waiter, long dueTime)
    {
        lock (Waiters)
        {
            waiter.ScheduledOn = _now.Ticks;
            waiter.WakeupTime = _now.Ticks + dueTime;
            _ = Waiters.Add(waiter);
        }

        WakeWaiters();
    }

    internal event EventHandler? GateOpening;

    private void WakeWaiters()
    {
        if (Interlocked.CompareExchange(ref _wakeWaitersGate, 1, 0) == 1)
        {
            // some other thread is already in here, so let it take care of things
            return;
        }

        while (true)
        {
            Waiter? candidate = null;
            lock (Waiters)
            {
                // find an expired waiter
                foreach (var waiter in Waiters)
                {
                    if (waiter.WakeupTime > _now.Ticks)
                    {
                        // not expired yet
                    }
                    else if (candidate is null)
                    {
                        // our first candidate
                        candidate = waiter;
                    }
                    else if (waiter.WakeupTime < candidate.WakeupTime)
                    {
                        // found a waiter with an earlier wake time, it's our new candidate
                        candidate = waiter;
                    }
                    else if (waiter.WakeupTime > candidate.WakeupTime)
                    {
                        // the waiter has a later wake time, so keep the current candidate
                    }
                    else if (waiter.ScheduledOn < candidate.ScheduledOn)
                    {
                        // the new waiter has the same wake time aa the candidate, pick whichever was scheduled earliest to maintain order
                        candidate = waiter;
                    }
                }

                if (candidate == null)
                {
                    // didn't find a candidate to wake, we're done
                    GateOpening?.Invoke(this, EventArgs.Empty);
                    _wakeWaitersGate = 0;
                    return;
                }
            }

            var oldTicks = _now.Ticks;

            // invoke the callback
            candidate.InvokeCallback();

            var newTicks = _now.Ticks;

            // see if we need to reschedule the waiter
            if (candidate.Period > 0)
            {
                // update the waiter's state
                candidate.ScheduledOn = newTicks;

                if (oldTicks != newTicks)
                {
                    // time changed while in the callback, readjust the wake time accordingly
                    candidate.WakeupTime = newTicks + candidate.Period;
                }
                else
                {
                    // move on to the next period
                    candidate.WakeupTime += candidate.Period;
                }
            }
            else
            {
                // this waiter is never running again, so remove from the set.
                RemoveWaiter(candidate);
            }
        }
    }
}

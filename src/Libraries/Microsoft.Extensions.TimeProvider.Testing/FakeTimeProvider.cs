// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Microsoft.Extensions.Time.Testing;

/// <summary>
/// A synthetic clock used to provide deterministic behavior in tests.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    internal readonly List<FakeTimeProviderTimer.Waiter> Waiters = new();

    private DateTimeOffset _now;
    private TimeZoneInfo _localTimeZone;

    /// <summary>
    /// Gets the time which was used as the starting point for the clock in this <see cref="FakeTimeProvider"/>.
    /// </summary>
    /// <remarks>
    /// This can be set by passing in a <see cref="DateTimeOffset"/> to the constructor
    /// which takes the <c>epoch</c> argument. If the default constructor is used,
    /// the clocks start time defaults to midnight January 1st 2000.
    /// </remarks>
    [Experimental]
    public DateTimeOffset Epoch { get; } = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeTimeProvider"/> class.
    /// </summary>
    /// <remarks>
    /// This creates a clock whose time is set to midnight January 1st 2000.
    /// The clock is set to not automatically advance every time it is read.
    /// </remarks>
    public FakeTimeProvider()
    {
        _localTimeZone = TimeZoneInfo.Utc;
        _now = Epoch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeTimeProvider"/> class.
    /// </summary>
    /// <param name="epoch">The starting point for the clock used by this <see cref="FakeTimeProvider"/>.</param>
    [Experimental]
    public FakeTimeProvider(DateTimeOffset epoch)
        : this()
    {
        Epoch = epoch;
        _now = epoch;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }

    /// <summary>
    /// Sets the date and time in the UTC timezone.
    /// </summary>
    /// <param name="value">The date and time in the UTC timezone.</param>
    public void SetUtcNow(DateTimeOffset value)
    {
        while (_now <= value && TryGetWaiterToWake(value) is FakeTimeProviderTimer.Waiter waiter)
        {
            _now = waiter.WakeupTime;
            waiter.TriggerAndSchedule(false);
        }

        _now = value;
    }

    /// <summary>
    /// Advances the clock's time by a specific amount.
    /// </summary>
    /// <param name="delta">The amount of time to advance the clock by.</param>
    public void Advance(TimeSpan delta)
        => SetUtcNow(_now + delta);

    /// <summary>
    /// Advances the clock's time by one millisecond.
    /// </summary>
    public void Advance() => Advance(TimeSpan.FromMilliseconds(1));

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
    /// Sets the local timezone.
    /// </summary>
    /// <param name="localTimeZone">The local timezone.</param>
    public void SetLocalTimeZone(TimeZoneInfo localTimeZone)
    {
        _localTimeZone = localTimeZone;
    }

    /// <summary>
    /// Gets the amount that the value from <see cref="GetTimestamp"/> increments per second.
    /// </summary>
    /// <remarks>
    /// We fix it here for test instability which would otherwise occur within
    /// <see cref="GetTimestamp"/> when the result of multiplying underlying ticks
    /// by frequency and dividing by ticks per second is truncated to long.
    ///
    /// Similarly truncation could occur when reversing this calculation to figure a time
    /// interval from the difference between two timestamps.
    ///
    /// As ticks per second is always 10^7, setting frequency to 10^7 is convenient.
    /// It happens that the system usually uses 10^9 or 10^6 so this could expose
    /// any assumption made that it is one of those values.
    /// </remarks>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <summary>
    /// Returns a string representation this clock's current time.
    /// </summary>
    /// <returns>A string representing the clock's current time.</returns>
    public override string ToString() => GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new FakeTimeProviderTimer(this, dueTime, period, callback, state);
    }

    internal void AddWaiter(FakeTimeProviderTimer.Waiter waiter)
    {
        lock (Waiters)
        {
            Waiters.Add(waiter);
        }
    }

    internal void RemoveWaiter(FakeTimeProviderTimer.Waiter waiter)
    {
        lock (Waiters)
        {
            _ = Waiters.Remove(waiter);
        }
    }

    private FakeTimeProviderTimer.Waiter? TryGetWaiterToWake(DateTimeOffset targetNow)
    {
        var candidate = default(FakeTimeProviderTimer.Waiter);

        lock (Waiters)
        {
            if (Waiters.Count == 0)
            {
                return null;
            }

            foreach (var waiter in Waiters)
            {
                if (waiter.WakeupTime > targetNow)
                {
                    continue;
                }

                if (candidate is null)
                {
                    candidate = waiter;
                    continue;
                }

                // This finds the waiter with the minimum WakeupTime and also ensures that if multiple waiters have the same
                // the one that is picked is also the one that was scheduled first.
                candidate = candidate.WakeupTime > waiter.WakeupTime
                        || (candidate.WakeupTime == waiter.WakeupTime && candidate.ScheduledOn > waiter.ScheduledOn)
                    ? waiter
                    : candidate;
            }
        }

        return candidate;
    }
}

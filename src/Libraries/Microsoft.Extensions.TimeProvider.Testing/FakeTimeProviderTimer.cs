// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Time.Testing;

internal sealed class FakeTimeProviderTimer : ITimer
{
    private Waiter? _waiter;

    public FakeTimeProviderTimer(FakeTimeProvider fakeTimeProvider, TimeSpan dueTime, TimeSpan period, TimerCallback callback, object? state)
    {
        _waiter = new Waiter(fakeTimeProvider, dueTime, period, callback, state);
        fakeTimeProvider.AddWaiter(_waiter);
        _waiter.TriggerAndSchedule(true);
    }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        if (_waiter == null)
        {
            return false;
        }

        _waiter.ChangeAndValidateDurations(dueTime, period);
        _waiter.TriggerAndSchedule(true);

        return true;
    }

    // In case the timer is not disposed, this will remove the Waiter instance from the provider.
    ~FakeTimeProviderTimer() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
#if NET5_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return default;
#endif
    }

    private void Dispose(bool _)
    {
        _waiter?.Dispose();
        _waiter = null;
    }

    // We keep all timer state here in order to prevent Timer instances from being self-referential,
    // which would block them being collected when someone forgets to call Dispose on the timer. With
    // this arrangement, the Timer object will always be collectible, which will end up calling Dispose
    // on this object due to the timer's finalizer.
    internal sealed class Waiter : IDisposable
    {
        private const uint MaxSupportedTimeout = 0xfffffffe;

        private readonly FakeTimeProvider _fakeTimeProvider;
        private readonly TimerCallback _callback;
        private readonly object? _state;

        private long _periodMs;
        private long _dueTimeMs;
        public DateTimeOffset WakeupTime { get; set; }

        public Waiter(FakeTimeProvider fakeTimeProvider, TimeSpan dueTime, TimeSpan period, TimerCallback callback, object? state)
        {
            _fakeTimeProvider = fakeTimeProvider;
            _callback = callback;
            _state = state;
            ChangeAndValidateDurations(dueTime, period);
        }

        public void ChangeAndValidateDurations(TimeSpan dueTime, TimeSpan period)
        {
            _dueTimeMs = (long)dueTime.TotalMilliseconds;
            _periodMs = (long)period.TotalMilliseconds;

#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
            _ = Throw.IfOutOfRange(_dueTimeMs, -1, MaxSupportedTimeout, nameof(dueTime));
            _ = Throw.IfOutOfRange(_periodMs, -1, MaxSupportedTimeout, nameof(period));
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly

        }

        public void TriggerAndSchedule(bool restart)
        {
            if (restart)
            {
                WakeupTime = DateTimeOffset.MaxValue;

                if (_dueTimeMs == 0)
                {
                    // If dueTime is zero, callback is invoked immediately.

                    _callback(_state);
                }
                else if (_dueTimeMs == Timeout.Infinite)
                {
                    // If dueTime is Timeout.Infinite, callback is not invoked; the timer is disabled.

                    return;
                }
                else
                {
                    // Schedule next event on dueTime

                    WakeupTime = _fakeTimeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(_dueTimeMs);

                    return;
                }
            }
            else
            {
                _callback(_state);
            }

            // Schedule next event based on Period

            if (_periodMs == 0 || _periodMs == Timeout.Infinite)
            {
                WakeupTime = DateTimeOffset.MaxValue;
            }
            else
            {
                WakeupTime = _fakeTimeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(_periodMs);
            }
        }

        public void Dispose()
        {
            _fakeTimeProvider.RemoveWaiter(this);
        }
    }
}

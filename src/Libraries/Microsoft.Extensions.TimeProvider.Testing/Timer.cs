// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Time.Testing;

// This implements the timer abstractions and is a thin wrapper around a waiter object.
// The main role of this type is to create the waiter, add it to the waiter list, and ensure it gets
// removed from the waiter list when the dispose is disposed or collected.
internal sealed class Timer : ITimer
{
    private const uint MaxSupportedTimeout = 0xfffffffe;

    private Waiter? _waiter;
    private FakeTimeProvider? _timeProvider;
    private TimerCallback? _callback;
    private object? _state;

    public Timer(FakeTimeProvider timeProvider, TimerCallback callback, object? state)
    {
        _timeProvider = timeProvider;
        _callback = callback;
        _state = state;
    }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        var dueTimeMs = (long)dueTime.TotalMilliseconds;
        var periodMs = (long)period.TotalMilliseconds;

#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
        _ = Throw.IfOutOfRange(dueTimeMs, -1, MaxSupportedTimeout, nameof(dueTime));
        _ = Throw.IfOutOfRange(periodMs, -1, MaxSupportedTimeout, nameof(period));
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly

        if (_timeProvider == null)
        {
            // timer has been disposed
            return false;
        }

        if (_waiter != null)
        {
            // remove any previous waiter
            _timeProvider.RemoveWaiter(_waiter);
            _waiter = null;
        }

        if (dueTimeMs < 0)
        {
            // this waiter will never wake up, so just bail
            return true;
        }

        if (periodMs < 0 || periodMs == Timeout.Infinite)
        {
            // normalize
            period = TimeSpan.Zero;
        }

        _waiter = new Waiter(_callback!, _state, period.Ticks);
        _timeProvider.AddWaiter(_waiter, dueTime.Ticks);
        return true;
    }

    // In case the timer is not disposed, this will remove the Waiter instance from the provider.
    ~Timer() => Dispose(false);

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
        if (_waiter != null)
        {
            _timeProvider!.RemoveWaiter(_waiter);
            _waiter = null;
        }

        _timeProvider = null;
        _callback = null;
        _state = null;
    }
}

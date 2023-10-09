// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.Extensions.TimeProvider.Testing;

// We keep all timer state here in order to prevent Timer instances from being self-referential,
// which would block them being collected when someone forgets to call Dispose on the timer. With
// this arrangement, the Timer object will always be collectible, which will end up calling Dispose
// on this object due to the timer's finalizer.
internal sealed class Waiter
{
    private readonly TimerCallback _callback;
    private readonly object? _state;

    public long ScheduledOn { get; set; } = -1;
    public long WakeupTime { get; set; } = -1;
    public long Period { get; }

    public Waiter(TimerCallback callback, object? state, long period)
    {
        _callback = callback;
        _state = state;
        Period = period;
    }

    public void InvokeCallback()
    {
        _callback(_state);
    }
}

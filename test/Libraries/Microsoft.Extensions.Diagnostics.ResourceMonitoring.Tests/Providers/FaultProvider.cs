// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.TimeProvider.Testing;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;

internal sealed class FaultProvider : ISnapshotProvider
{
    private readonly FakeTimeProvider _clock = new();

    public bool ShouldThrow { get; set; } = true;

    public SystemResources Resources => new(1.0, 1.0, 1000, 1000);

    public Snapshot GetSnapshot()
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException();
        }

        // return a dummy value.
        return new Snapshot(TimeSpan.FromTicks(_clock.GetUtcNow().Ticks), TimeSpan.Zero, TimeSpan.Zero, ulong.MaxValue);
    }
}

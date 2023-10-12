// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;

internal sealed class FakeProvider : ISnapshotProvider
{
    private Snapshot _snapshot = new(
            TimeSpan.FromTicks(new FakeTimeProvider().GetUtcNow().Ticks),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            500);

    public SystemResources Resources => new(1.0, 1.0, 1000, 1000);

    public Snapshot GetSnapshot()
    {
        return _snapshot;
    }

    public void SetNextSnapshot(Snapshot snapshot)
    {
        _snapshot = snapshot;
    }
}

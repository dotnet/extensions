// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
public class NullSnapshotProviderTest
{
    private const double CpuUnits = 1.0;
    private const ulong MemoryTotalInBytes = long.MaxValue;
    private const ulong MemoryUsageInBytes = 0UL;

    private static readonly FakeTimeProvider _clock = new();

    [Fact]
    public void BasicConstructor_InitializesResourcesProperty()
    {
        var provider = new NullSnapshotProvider();

        Assert.Equal(CpuUnits, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(CpuUnits, provider.Resources.MaximumCpuUnits);
        Assert.Equal(MemoryTotalInBytes, provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(MemoryTotalInBytes, provider.Resources.MaximumMemoryInBytes);
    }

    [Fact]
    public void GetSnapshot_GetsProperSnapshot()
    {
        var provider = new NullSnapshotProvider(_clock);

        var snapshot = provider.GetSnapshot();

        Assert.Equal(TimeSpan.FromTicks(_clock.GetUtcNow().Ticks), snapshot.TotalTimeSinceStart);
        Assert.Equal(TimeSpan.Zero, snapshot.KernelTimeSinceStart);
        Assert.Equal(TimeSpan.Zero, snapshot.UserTimeSinceStart);
        Assert.Equal(MemoryUsageInBytes, snapshot.MemoryUsageInBytes);
    }
}

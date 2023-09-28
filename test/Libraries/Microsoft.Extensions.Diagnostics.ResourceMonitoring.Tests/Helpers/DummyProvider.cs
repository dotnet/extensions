// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;

internal class DummyProvider : ISnapshotProvider
{
    public static readonly double CpuUnits = 4;
    public static readonly TimeSpan KernelTimeSinceStart = TimeSpan.FromTicks(1000);
    public static readonly ulong MemoryTotalInBytes = 1024;
    public static readonly ulong MemoryUsageInBytes = 512;
    public static readonly FakeTimeProvider SnapshotTimeClock = new();
    public static readonly double TotalCoreLimitPercentage = 100.0;
    public static readonly TimeSpan UserTimeSinceStart = TimeSpan.FromTicks(1000);

    public SystemResources Resources => new(
        DummyProvider.CpuUnits,
        DummyProvider.CpuUnits,
        DummyProvider.MemoryTotalInBytes,
        DummyProvider.MemoryTotalInBytes);

    public Snapshot GetSnapshot()
    {
        return new Snapshot(
            totalTimeSinceStart: TimeSpan.FromTicks(SnapshotTimeClock.GetUtcNow().Ticks),
            kernelTimeSinceStart: DummyProvider.KernelTimeSinceStart,
            userTimeSinceStart: DummyProvider.UserTimeSinceStart,
            memoryUsageInBytes: DummyProvider.MemoryUsageInBytes);
    }
}

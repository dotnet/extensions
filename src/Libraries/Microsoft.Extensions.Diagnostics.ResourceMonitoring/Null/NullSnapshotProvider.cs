// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class NullSnapshotProvider : ISnapshotProvider
{
    private const double AvailableCpuUnits = 1.0;
    private const ulong MemoryTotalInBytes = long.MaxValue;
    private const ulong MemoryUsageInBytes = 0UL;

    private readonly TimeProvider _timeProvider;

    public NullSnapshotProvider()
        : this(TimeProvider.System)
    {
    }

    internal NullSnapshotProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public SystemResources Resources { get; } = new(AvailableCpuUnits, AvailableCpuUnits, MemoryTotalInBytes, MemoryTotalInBytes);

    public ResourceUtilizationSnapshot GetSnapshot()
        => new(TimeSpan.FromTicks(_timeProvider.GetUtcNow().Ticks), TimeSpan.Zero, TimeSpan.Zero, MemoryUsageInBytes);
}

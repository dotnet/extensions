// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.TimeProvider.Testing;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;

internal sealed class ConditionallyFaultProvider : ISnapshotProvider
{
    private const double CpuUnits = 1.0;
    private const ulong TotalMemory = 1000UL;
    private const ulong UsedMemory = 100;

    private readonly Guid _errorGuid;

    public bool CanThrow { get; set; }

    public ConditionallyFaultProvider(Guid errorGuid)
    {
        _errorGuid = errorGuid;
        CanThrow = false;
    }

    public SystemResources Resources => new(CpuUnits, CpuUnits, TotalMemory, TotalMemory);

    public Snapshot GetSnapshot()
    {
        if (CanThrow)
        {
            throw new InvalidOperationException(_errorGuid.ToString());
        }

        return new Snapshot(
            TimeSpan.FromTicks(new FakeTimeProvider().GetUtcNow().Ticks),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            UsedMemory);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class IResourceUtilizationSnapshotProviderTests
{
    [Fact]
    public void Resources_Gets_ValidSystemResources()
    {
        var provider = new DummyProvider();
        Assert.Equal(DummyProvider.CpuUnits, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(DummyProvider.CpuUnits, provider.Resources.MaximumCpuUnits);
        Assert.Equal(DummyProvider.MemoryTotalInBytes, provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(DummyProvider.MemoryTotalInBytes, provider.Resources.MaximumMemoryInBytes);
    }

    [Fact]
    public void GetSnapshot_Returns_ValidResourceUilizationSnapshot()
    {
        var snapshot = new DummyProvider().GetSnapshot();
        Assert.Equal(DummyProvider.KernelTimeSinceStart, snapshot.KernelTimeSinceStart);
        Assert.Equal(DummyProvider.MemoryUsageInBytes, snapshot.MemoryUsageInBytes);
        Assert.Equal(DummyProvider.SnapshotTimeClock.GetUtcNow().Ticks, snapshot.TotalTimeSinceStart.Ticks);
        Assert.Equal(DummyProvider.UserTimeSinceStart, snapshot.UserTimeSinceStart);
    }
}

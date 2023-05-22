// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class IResourceUtilizationTrackerTest
{
    [Fact]
    public void GetAverageUtilization_Gets_ValidUtilization()
    {
        var tracker = new DummyTracker();
        var utilization = tracker.GetUtilization(TimeSpan.Zero);
        Assert.Equal(DummyTracker.CpuPercentage, utilization.CpuUsedPercentage);
        Assert.Equal(DummyTracker.MemoryPercentage, utilization.MemoryUsedPercentage);
        Assert.Equal(DummyTracker.MemoryUsed, utilization.MemoryUsedInBytes);
        Assert.Equal(DummyTracker.MemoryTotal, utilization.SystemResources.GuaranteedMemoryInBytes);
        Assert.Equal(DummyTracker.MemoryTotal, utilization.SystemResources.MaximumMemoryInBytes);
        Assert.Equal(DummyTracker.CpuUnits, utilization.SystemResources.GuaranteedCpuUnits);
        Assert.Equal(DummyTracker.CpuUnits, utilization.SystemResources.MaximumCpuUnits);
    }
}

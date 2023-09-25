// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class ResourceUtilizationTests
{
    private const double CpuPercentage = 50.0;
    private const ulong MemoryUsed = 100;
    private const ulong MemoryTotal = 1000;
    private const double CpuUnits = 1.0;
    private readonly SystemResources _systemResources = new(CpuUnits, CpuUnits, MemoryTotal, MemoryTotal);

    [Fact]
    public void BasicConstructor()
    {
        var utilization = new ResourceUtilization(CpuPercentage, MemoryUsed, _systemResources);
        Assert.Equal(CpuPercentage, utilization.CpuUsedPercentage);
        Assert.Equal(MemoryUsed, utilization.MemoryUsedInBytes);
        Assert.Equal(Math.Min(1.0, (double)MemoryUsed / MemoryTotal) * 100.0, utilization.MemoryUsedPercentage);

        var snap = utilization.Snapshot;
        Assert.Equal(default, snap.TotalTimeSinceStart);
        Assert.Equal(default, snap.KernelTimeSinceStart);
    }

    [Fact]
    public void Constructor_ProvidedNegativeCpuUtilization_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceUtilization(-50.0, 500, _systemResources));
    }
}

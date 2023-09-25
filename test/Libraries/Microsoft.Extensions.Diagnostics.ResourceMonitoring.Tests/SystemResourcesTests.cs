// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class SystemResourcesTests
{
    [Fact]
    public void BasicConstructor()
    {
        const double CpuUnits = 1.0;
        const uint MemoryTotalInBytes = 1000U;

        var systemResources = new SystemResources(CpuUnits, CpuUnits, MemoryTotalInBytes, MemoryTotalInBytes);

        Assert.Equal(CpuUnits, systemResources.GuaranteedCpuUnits);
        Assert.Equal(CpuUnits, systemResources.MaximumCpuUnits);
        Assert.Equal(MemoryTotalInBytes, systemResources.GuaranteedMemoryInBytes);
        Assert.Equal(MemoryTotalInBytes, systemResources.MaximumMemoryInBytes);
    }

    [Fact]
    public void Constructor_ProvidedInvalidParameters_Throws()
    {
        // Zero Cpu Units
        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemResources(0.0, 1.0, 1000UL, 1000UL));

        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemResources(1.0, 0.0, 1000UL, 1000UL));

        // Zero Memory
        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemResources(1.0, 1.0, 0UL, 1000UL));

        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemResources(1.0, 1.0, 1000UL, 0UL));
    }
}

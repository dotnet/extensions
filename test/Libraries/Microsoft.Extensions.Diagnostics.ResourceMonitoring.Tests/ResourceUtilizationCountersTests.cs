// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class ResourceUtilizationCountersTests
{
    [Fact]
    public void ExpectedStrings()
    {
        Assert.Equal("cpu_consumption_percentage", ResourceUtilizationCounters.CpuConsumptionPercentage);
        Assert.Equal("memory_consumption_percentage", ResourceUtilizationCounters.MemoryConsumptionPercentage);
    }
}

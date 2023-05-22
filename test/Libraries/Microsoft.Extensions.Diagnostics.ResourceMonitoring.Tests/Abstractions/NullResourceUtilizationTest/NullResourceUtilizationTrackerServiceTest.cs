// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.NullImplementationTest;

public sealed class NullResourceUtilizationTrackerServiceTest
{
    private const double CpuUnits = 1.0;
    private static readonly SystemResources _systemResources = new(CpuUnits, CpuUnits, long.MaxValue, long.MaxValue);
    private static readonly Utilization _utilization = new(0.0, 0U, _systemResources);

    [Fact]
    public void GetAverageUtilization_ReturnsFixedUtilizationValue()
    {
        var tracker = new NullResourceUtilizationTrackerService();

        Assert.Equal(_utilization, tracker.GetUtilization(TimeSpan.Zero));
        Assert.Equal(_utilization, tracker.GetUtilization(TimeSpan.FromSeconds(1)));
        Assert.Equal(_utilization, tracker.GetUtilization(TimeSpan.FromMinutes(1)));
    }
}

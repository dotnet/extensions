// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class ResourceUtilizationSnapshotTest
{
    [Fact]
    public void BasicInitializaiton()
    {
        var time = new FakeTimeProvider();

        // Constructor provided TimeSpan
        var snapshot = new ResourceUtilizationSnapshot(TimeSpan.FromTicks(time.GetUtcNow().Ticks), TimeSpan.Zero, TimeSpan.FromSeconds(1), 10);
        Assert.Equal(time.GetUtcNow().Ticks, snapshot.TotalTimeSinceStart.Ticks);

        // Constructor provided IClock
        snapshot = new ResourceUtilizationSnapshot(time, TimeSpan.Zero, TimeSpan.FromSeconds(1), 10);
        Assert.Equal(time.GetUtcNow().Ticks, snapshot.TotalTimeSinceStart.Ticks);
    }

    [Fact]
    public void Constructor_ProvidedWithNegativeValueOfKernelTimeSinceStart_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()
            => new ResourceUtilizationSnapshot(new FakeTimeProvider(), TimeSpan.MinValue, TimeSpan.FromSeconds(1), 1000));
    }

    [Fact]
    public void Constructor_ProvidedWithNegativeValueOfUserTimeSinceStart_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()
            => new ResourceUtilizationSnapshot(new FakeTimeProvider(), TimeSpan.Zero, TimeSpan.MinValue, 1000));
    }
}

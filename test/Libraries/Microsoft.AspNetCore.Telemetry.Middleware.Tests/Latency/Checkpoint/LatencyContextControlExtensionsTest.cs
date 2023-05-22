// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Telemetry;
using Microsoft.Extensions.Telemetry.Latency;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class LatencyContextControlExtensionsTest
{
    [Fact]
    public void TryGetCheckpoint_ReturnsTrue_WhenPresent()
    {
        var cc = new Mock<ILatencyContext>();
        var ld = GetLatencyData();
        cc.Setup(cc => cc.LatencyData).Returns(ld);

        Assert.True(cc.Object.TryGetCheckpoint("ca", out var elapsed1, out var elapsed1Freq));
    }

    [Fact]
    public void TryGetCheckpoint_ReturnsFalse_WhenAbsent()
    {
        var cc = new Mock<ILatencyContext>();
        var ld = GetLatencyData();
        cc.Setup(cc => cc.LatencyData).Returns(ld);

        Assert.False(cc.Object.TryGetCheckpoint("not", out var elapsed2, out var elapsed2Freq));
    }

    private static LatencyData GetLatencyData()
    {
        var checkpoints = new[] { new Checkpoint("ca", default, default) };
        var chkSegment = new ArraySegment<Checkpoint>(checkpoints);
        return new LatencyData(default, chkSegment, default, default, default);
    }
}

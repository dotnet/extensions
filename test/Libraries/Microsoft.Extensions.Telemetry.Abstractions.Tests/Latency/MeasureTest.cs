// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Telemetry.Latency.Test;

public class MeasureTest
{
    [Fact]
    public void Measure_BasicTest()
    {
        string name = "Name";
        long value = 10;
        var c = new Measure(name, value);
        Assert.Equal(c.Name, name);
        Assert.Equal(c.Value, value);
    }

    [Fact]
    public void Measure_EqualsCheck()
    {
        string name = "Name";
        long value = 100;
        var m1 = new Measure(name, value);
        var m2 = new Measure(name, value);
        var m3 = new Measure("Diff", value);
        var m4 = new Measure(name, 150);
        Assert.True(m1.Equals(m2));
        Assert.True(m1.Equals((object)m2));
        Assert.False(m1.Equals(m3));
        Assert.False(m1.Equals(m4));
        Assert.False(m1.Equals(null));
        Assert.True(m1 == m2);
        Assert.False(m1 != m2);
        Assert.True(m1.GetHashCode() == m2.GetHashCode());
    }

    [Fact]
    public void MeasureToken_BasicTest()
    {
        string name = "Name";
        int pos = 10;
        var c = new MeasureToken(name, pos);
        Assert.Equal(c.Name, name);
        Assert.Equal(c.Position, pos);
    }
}

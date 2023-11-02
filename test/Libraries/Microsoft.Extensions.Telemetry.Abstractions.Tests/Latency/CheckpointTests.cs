// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class CheckpointTests
{
    [Fact]
    public void Checkpoint_BasicTest()
    {
        string name = "Name";
        var c = new Checkpoint(name, 10_000_000_000, 10_000_000_000);
        Assert.Equal(name, c.Name);
        Assert.Equal(10_000_000_000, c.Elapsed);
        Assert.Equal(10_000_000_000, c.Frequency);
    }

    [Fact]
    public void Checkpoint_EqualsCheck()
    {
        string name = "Name";
        var c1 = new Checkpoint(name, 1000, 1000);
        var c2 = new Checkpoint(name, 1000, 1000);
        var c3 = new Checkpoint("Diff", 1000, 1000);
        var c4 = new Checkpoint(name, 2000, 1000);
        Assert.True(c1.Equals(c2));
        Assert.True(c1.Equals((object)c2));
        Assert.False(c1.Equals(c3));
        Assert.False(c1.Equals(c4));
        Assert.False(c1.Equals(null));
        Assert.True(c1 == c2);
        Assert.False(c1 != c2);
        Assert.False(c1 == c3);
        Assert.True(c1.GetHashCode() == c2.GetHashCode());
    }

    [Fact]
    public void CheckpointToken_BasicTest()
    {
        string name = "Name";
        int pos = 5;
        var c = new CheckpointToken(name, pos);
        Assert.Equal(name, c.Name);
        Assert.Equal(pos, c.Position);
    }
}

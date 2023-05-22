// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;
public class AsyncStateTokenTests
{
    [Fact]
    public void AsyncStateToekn_Equal()
    {
        var t1 = new AsyncStateToken(1);
        var t2 = new AsyncStateToken(1);

        Assert.True(t1.Equals(t2));
        Assert.True((object)t1 != (object)t2);
        Assert.True(t1.Equals((object)t2));
        Assert.False(t1.Equals(string.Empty));
        Assert.True(t1 == t2);
        Assert.False(t1 != t2);
    }

    [Fact]
    public void AsyncStateToekn_NotEqual()
    {
        var t1 = new AsyncStateToken(1);
        var t2 = new AsyncStateToken(2);

        Assert.False(t1.Equals(t2));
        Assert.True((object)t1 != (object)t2);
        Assert.False(t1.Equals((object)t2));
        Assert.False(t1 == t2);
        Assert.True(t1 != t2);
    }

    [Fact]
    public void AsyncStateToekn_HashCode()
    {
        int ind = 1;
        var t1 = new AsyncStateToken(ind);

        Assert.Equal(ind.GetHashCode(), t1.GetHashCode());
    }
}

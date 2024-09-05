// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if !NET8_0_OR_GREATER

using System;
using Xunit;

namespace Microsoft.Shared.StringSplit.Test;

public static class StringRangeTests
{
    [Fact]
    public static void Operators()
    {
        var ss = new StringRange(1, 2);
        var ss2 = new StringRange(2, 2);

        Assert.True(ss.Equals(ss));
        Assert.True(ss.Equals(new StringRange(1, 2)));
        Assert.True(ss.Equals((object)ss));
        Assert.False(ss.Equals(new object()));
        Assert.False(ss.Equals(new StringRange(1, 3)));

        Assert.Equal(ss.GetHashCode(), ss.GetHashCode());

        Assert.True(ss == new StringRange(1, 2));
        Assert.True(ss != ss2);
        Assert.True(ss.CompareTo(ss2) < 0);
        Assert.True(ss.CompareTo((object)ss2) < 0);
        Assert.True(ss.CompareTo(null) == 1);
        Assert.True(ss < ss2);
        Assert.True(ss <= ss2);
        Assert.True(ss <= new StringRange(1, 2));
        Assert.True(ss2 > ss);
        Assert.True(ss2 >= ss);
        Assert.True(ss2 >= new StringRange(2, 2));

        Assert.Throws<ArgumentException>(() => ss.CompareTo(new object()));
    }
}

#endif

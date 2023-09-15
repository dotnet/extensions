// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class TagTest
{
    [Fact]
    public void Tag_BasicTest()
    {
        string name = "Name";
        string value = "Val";
        var t = new Tag(name, value);
        Assert.Equal(t.Name, name);
        Assert.Equal(t.Value, value);
    }

    [Fact]
    public void TagToken_BasicTest()
    {
        string name = "Name";
        int pos = 10;
        var c = new TagToken(name, pos);
        Assert.Equal(c.Name, name);
        Assert.Equal(c.Position, pos);
    }
}

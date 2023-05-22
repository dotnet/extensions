// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HostHeaderValueTests
{
    [Fact]
    public void EqualsTest()
    {
        var host1 = new HostHeaderValue("localhost", 80);
        var sameAsHost1 = new HostHeaderValue("localhost", 80);
        var differentHost = new HostHeaderValue("127.0.0.1", 80);
        var differentPort = new HostHeaderValue("localhost", 443);

        Assert.Equal(sameAsHost1, host1);
        Assert.NotEqual(differentHost, host1);
        Assert.NotEqual(differentPort, host1);
    }

    [Fact]
    public void ObjectEqualsTest()
    {
        var host1 = new HostHeaderValue("localhost", 80);
        object sameAsHost1 = new HostHeaderValue("localhost", 80);
        object differentHost = new HostHeaderValue("127.0.0.1", 80);
        object differentPort = new HostHeaderValue("localhost", 443);

#pragma warning disable IDE0004 // Remove Unnecessary Cast
        Assert.True(sameAsHost1.Equals((object)host1));
        Assert.False(differentHost.Equals((object)host1));
        Assert.False(differentPort.Equals((object)host1));
        Assert.False(differentPort.Equals(new object()));
#pragma warning restore IDE0004 // Remove Unnecessary Cast

        Assert.NotEqual(new object(), host1);
    }

    [Fact]
    public void EqualsOperatorsTest()
    {
        var host1 = new HostHeaderValue("localhost", 80);
        var sameAsHost1 = new HostHeaderValue("localhost", 80);
        var differentHost = new HostHeaderValue("127.0.0.1", 80);
        var differentPort = new HostHeaderValue("localhost", 443);

        Assert.True(host1 == sameAsHost1);
        Assert.False(host1 == differentHost);
        Assert.False(host1 == differentPort);

        Assert.False(host1 != sameAsHost1);
        Assert.True(host1 != differentHost);
        Assert.True(host1 != differentPort);
    }

    [Fact]
    public void GetHashCodeTest()
    {
        var host1HashCode = new HostHeaderValue("localhost", 80).GetHashCode();
        var sameAsHost1HashCode = new HostHeaderValue("localhost", 80).GetHashCode();
        var differentHostHashCode = new HostHeaderValue("127.0.0.1", 80).GetHashCode();
        var differentPortHashCode = new HostHeaderValue("localhost", 443).GetHashCode();

        Assert.Equal(sameAsHost1HashCode, host1HashCode);
        Assert.NotEqual(differentHostHashCode, host1HashCode);
        Assert.NotEqual(differentPortHashCode, host1HashCode);
    }

    [Fact]
    public void ToStringTest()
    {
        var hhv = new HostHeaderValue("foo", null);
        Assert.Equal("foo", hhv.ToString());

        hhv = new HostHeaderValue("foo", 82);
        Assert.Equal("foo:82", hhv.ToString());
    }

    [Fact]
    public void Invalid()
    {
        Assert.False(HostHeaderValue.TryParse(string.Empty, out var _));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeSessionExtensionsTests
{
    [Fact]
    public void GetService_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("session", () => ((IRealtimeSession)null!).GetService<IRealtimeSession>());
    }

    [Fact]
    public void GetService_ReturnsMatchingService()
    {
        using var session = new TestRealtimeSession();
        var result = session.GetService<TestRealtimeSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public void GetService_ReturnsNullForNonMatchingType()
    {
        using var session = new TestRealtimeSession();
        var result = session.GetService<string>();
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var session = new TestRealtimeSession();
        var result = session.GetService<TestRealtimeSession>("someKey");
        Assert.Null(result);
    }

    [Fact]
    public void GetService_ReturnsInterfaceType()
    {
        using var session = new TestRealtimeSession();
        var result = session.GetService<IRealtimeSession>();
        Assert.Same(session, result);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeSessionExtensionsTests
{
    [Fact]
    public void GetService_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("session", () => ((IRealtimeClientSession)null!).GetService<IRealtimeClientSession>());
    }

    [Fact]
    public async Task GetService_ReturnsMatchingService()
    {
        await using var session = new TestRealtimeSession();
        var result = session.GetService<TestRealtimeSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public async Task GetService_ReturnsNullForNonMatchingType()
    {
        await using var session = new TestRealtimeSession();
        var result = session.GetService<string>();
        Assert.Null(result);
    }

    [Fact]
    public async Task GetService_WithServiceKey_ReturnsNull()
    {
        await using var session = new TestRealtimeSession();
        var result = session.GetService<TestRealtimeSession>("someKey");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetService_ReturnsInterfaceType()
    {
        await using var session = new TestRealtimeSession();
        var result = session.GetService<IRealtimeClientSession>();
        Assert.Same(session, result);
    }
}

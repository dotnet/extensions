// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeClientSessionExtensionsTests
{
    [Fact]
    public void GetService_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("session", () => ((IRealtimeClientSession)null!).GetService<IRealtimeClientSession>());
    }

    [Fact]
    public async Task GetService_ReturnsMatchingService()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetService<TestRealtimeClientSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public async Task GetService_ReturnsNullForNonMatchingType()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetService<string>();
        Assert.Null(result);
    }

    [Fact]
    public async Task GetService_WithServiceKey_ReturnsNull()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetService<TestRealtimeClientSession>("someKey");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetService_ReturnsInterfaceType()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetService<IRealtimeClientSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public void GetRequiredService_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("session", () => ((IRealtimeClientSession)null!).GetRequiredService(typeof(string)));
        Assert.Throws<ArgumentNullException>("session", () => ((IRealtimeClientSession)null!).GetRequiredService<string>());
    }

    [Fact]
    public async Task GetRequiredService_NullServiceType_Throws()
    {
        await using var session = new TestRealtimeClientSession();
        Assert.Throws<ArgumentNullException>("serviceType", () => session.GetRequiredService(null!));
    }

    [Fact]
    public async Task GetRequiredService_ReturnsMatchingService()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetRequiredService<TestRealtimeClientSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public async Task GetRequiredService_ReturnsInterfaceType()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetRequiredService<IRealtimeClientSession>();
        Assert.Same(session, result);
    }

    [Fact]
    public async Task GetRequiredService_NonGeneric_ReturnsMatchingService()
    {
        await using var session = new TestRealtimeClientSession();
        var result = session.GetRequiredService(typeof(TestRealtimeClientSession));
        Assert.Same(session, result);
    }

    [Fact]
    public async Task GetRequiredService_ThrowsForNonMatchingType()
    {
        await using var session = new TestRealtimeClientSession();
        Assert.Throws<InvalidOperationException>(() => session.GetRequiredService<string>());
    }

    [Fact]
    public async Task GetRequiredService_NonGeneric_ThrowsForNonMatchingType()
    {
        await using var session = new TestRealtimeClientSession();
        Assert.Throws<InvalidOperationException>(() => session.GetRequiredService(typeof(string)));
    }

    [Fact]
    public async Task GetRequiredService_WithServiceKey_ThrowsForNonMatchingKey()
    {
        await using var session = new TestRealtimeClientSession();
        Assert.Throws<InvalidOperationException>(() => session.GetRequiredService<TestRealtimeClientSession>("someKey"));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeSessionTests
{
    [Fact]
    public async Task GetService_ReturnsExpectedServices()
    {
        await using IRealtimeClientSession session = new OpenAIRealtimeSession("key", "model");

        Assert.Same(session, session.GetService(typeof(OpenAIRealtimeSession)));
        Assert.Same(session, session.GetService(typeof(IRealtimeClientSession)));
        Assert.Null(session.GetService(typeof(string)));
        Assert.Null(session.GetService(typeof(OpenAIRealtimeSession), "someKey"));
    }

    [Fact]
    public async Task GetService_NullServiceType_Throws()
    {
        await using IRealtimeClientSession session = new OpenAIRealtimeSession("key", "model");
        Assert.Throws<ArgumentNullException>("serviceType", () => session.GetService(null!));
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        IRealtimeClientSession session = new OpenAIRealtimeSession("key", "model");
        await session.DisposeAsync();

        // Second dispose should not throw.
        await session.DisposeAsync();
        Assert.Null(session.GetService(typeof(string)));
    }

    [Fact]
    public async Task Options_InitiallyNull()
    {
        await using var session = new OpenAIRealtimeSession("key", "model");
        Assert.Null(session.Options);
    }

    [Fact]
    public void SessionUpdateMessage_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>("options", () => new RealtimeClientSessionUpdateMessage(null!));
    }

    [Fact]
    public async Task SendAsync_NullMessage_Throws()
    {
        await using var session = new OpenAIRealtimeSession("key", "model");
        await Assert.ThrowsAsync<ArgumentNullException>("message", () => session.SendAsync(null!));
    }

    [Fact]
    public async Task SendAsync_CancelledToken_ReturnsSilently()
    {
        await using var session = new OpenAIRealtimeSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw when cancellation is requested.
        await session.SendAsync(new RealtimeClientMessage(), cts.Token);
        Assert.Null(session.Options);
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_Throws()
    {
        await using var session = new OpenAIRealtimeSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ConnectAsync(cts.Token));
    }
}

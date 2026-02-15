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
    public void GetService_ReturnsExpectedServices()
    {
        using IRealtimeSession session = new OpenAIRealtimeSession("key", "model");

        Assert.Same(session, session.GetService(typeof(OpenAIRealtimeSession)));
        Assert.Same(session, session.GetService(typeof(IRealtimeSession)));
        Assert.Null(session.GetService(typeof(string)));
        Assert.Null(session.GetService(typeof(OpenAIRealtimeSession), "someKey"));
    }

    [Fact]
    public void GetService_NullServiceType_Throws()
    {
        using IRealtimeSession session = new OpenAIRealtimeSession("key", "model");
        Assert.Throws<ArgumentNullException>("serviceType", () => session.GetService(null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        IRealtimeSession session = new OpenAIRealtimeSession("key", "model");
        session.Dispose();

        // Second dispose should not throw.
        session.Dispose();
        Assert.Null(session.GetService(typeof(string)));
    }

    [Fact]
    public void Options_InitiallyNull()
    {
        using var session = new OpenAIRealtimeSession("key", "model");
        Assert.Null(session.Options);
    }

    [Fact]
    public async Task UpdateAsync_NullOptions_Throws()
    {
        using var session = new OpenAIRealtimeSession("key", "model");
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => session.UpdateAsync(null!));
    }

    [Fact]
    public async Task InjectClientMessageAsync_NullMessage_Throws()
    {
        using var session = new OpenAIRealtimeSession("key", "model");
        await Assert.ThrowsAsync<ArgumentNullException>("message", () => session.InjectClientMessageAsync(null!));
    }

    [Fact]
    public async Task InjectClientMessageAsync_CancelledToken_ReturnsSilently()
    {
        using var session = new OpenAIRealtimeSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw when cancellation is requested.
        await session.InjectClientMessageAsync(new RealtimeClientMessage(), cts.Token);
        Assert.Null(session.Options);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_NullUpdates_Throws()
    {
        using var session = new OpenAIRealtimeSession("key", "model");

        await Assert.ThrowsAsync<ArgumentNullException>("updates", async () =>
        {
            await foreach (var msg in session.GetStreamingResponseAsync(null!))
            {
                _ = msg;
            }
        });
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_ReturnsFalse()
    {
        using var session = new OpenAIRealtimeSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await session.ConnectAsync(cts.Token);
        Assert.False(result);
    }
}

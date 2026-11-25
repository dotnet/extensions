// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable OPENAI002 // OpenAI Realtime API is experimental

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeClientSessionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToRealtimeItem_UsesCompactJsonForFunctionPayloads(bool arguments)
    {
        using var payload = JsonDocument.Parse("""{"query":"notebook","max":10}""");
        AIContent content = arguments
            ? new FunctionCallContent("call_1", "search", new Dictionary<string, object?>
            {
                ["query"] = payload.RootElement.GetProperty("query"),
                ["max"] = payload.RootElement.GetProperty("max"),
            })
            : new FunctionResultContent("call_1", payload.RootElement);
        var item = new RealtimeConversationItem([content]);

        var converted = Assert.IsAssignableFrom<OpenAI.Realtime.RealtimeItem>(
            typeof(OpenAIRealtimeClientSession).GetMethod("ToRealtimeItem", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [item]));
        using var json = JsonDocument.Parse(ModelReaderWriter.Write(converted, new ModelReaderWriterOptions("W")).ToMemory());
        Assert.Equal("""{"query":"notebook","max":10}""", json.RootElement.GetProperty(arguments ? "arguments" : "output").GetString());
        Assert.Equal("call_1", json.RootElement.GetProperty("call_id").GetString());
    }

    [Fact]
    public async Task GetService_ReturnsExpectedServices()
    {
        await using IRealtimeClientSession session = new OpenAIRealtimeClientSession("key", "model");

        Assert.Same(session, session.GetService(typeof(OpenAIRealtimeClientSession)));
        Assert.Same(session, session.GetService(typeof(IRealtimeClientSession)));
        Assert.Null(session.GetService(typeof(string)));
        Assert.Null(session.GetService(typeof(OpenAIRealtimeClientSession), "someKey"));
    }

    [Fact]
    public async Task GetService_NullServiceType_Throws()
    {
        await using IRealtimeClientSession session = new OpenAIRealtimeClientSession("key", "model");
        Assert.Throws<ArgumentNullException>("serviceType", () => session.GetService(null!));
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        IRealtimeClientSession session = new OpenAIRealtimeClientSession("key", "model");
        await session.DisposeAsync();

        // Second dispose should not throw.
        await session.DisposeAsync();
        Assert.Null(session.GetService(typeof(string)));
    }

    [Fact]
    public async Task Options_InitiallyNull()
    {
        await using var session = new OpenAIRealtimeClientSession("key", "model");
        Assert.Null(session.Options);
    }

    [Fact]
    public void SessionUpdateMessage_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>("options", () => new SessionUpdateRealtimeClientMessage(null!));
    }

    [Fact]
    public async Task SendAsync_NullMessage_Throws()
    {
        await using var session = new OpenAIRealtimeClientSession("key", "model");
        await Assert.ThrowsAsync<ArgumentNullException>("message", () => session.SendAsync(null!));
    }

    [Fact]
    public async Task SendAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        await using var session = new OpenAIRealtimeClientSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should throw when cancellation is requested.
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.SendAsync(new RealtimeClientMessage(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_NotConnected_ThrowsInvalidOperationException()
    {
        await using var session = new OpenAIRealtimeClientSession("key", "model");

        // Should throw when session is not connected.
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SendAsync(new RealtimeClientMessage()));
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_Throws()
    {
        await using var session = new OpenAIRealtimeClientSession("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => session.ConnectAsync(cts.Token));
        Assert.True(
            ex is OperationCanceledException || ex is System.Net.WebSockets.WebSocketException,
            $"Expected OperationCanceledException or WebSocketException but got {ex.GetType().FullName}");
    }
}

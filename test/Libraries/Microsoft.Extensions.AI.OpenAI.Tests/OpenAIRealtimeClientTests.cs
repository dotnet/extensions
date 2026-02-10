// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeClientTests
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("apiKey", () => new OpenAIRealtimeClient(null!, "model"));
        Assert.Throws<ArgumentNullException>("model", () => new OpenAIRealtimeClient("key", null!));
    }

    [Fact]
    public void AsIRealtimeClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsIRealtimeClient("key", "model"));

        var client = new OpenAIClient(new ApiKeyCredential("key"));
        Assert.Throws<ArgumentNullException>("apiKey", () => client.AsIRealtimeClient(null!, "model"));
        Assert.Throws<ArgumentNullException>("model", () => client.AsIRealtimeClient("key", null!));
    }

    [Fact]
    public void AsIRealtimeClient_ReturnsIRealtimeClient()
    {
        var openAIClient = new OpenAIClient(new ApiKeyCredential("key"));
        using var client = openAIClient.AsIRealtimeClient("key", "gpt-realtime");
        Assert.NotNull(client);
        Assert.IsType<OpenAIRealtimeClient>(client);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        using IRealtimeClient client = new OpenAIRealtimeClient("key", "model");

        Assert.Same(client, client.GetService(typeof(OpenAIRealtimeClient)));
        Assert.Same(client, client.GetService(typeof(IRealtimeClient)));
        Assert.Null(client.GetService(typeof(string)));
        Assert.Null(client.GetService(typeof(OpenAIRealtimeClient), "someKey"));
    }

    [Fact]
    public void GetService_NullServiceType_Throws()
    {
        using IRealtimeClient client = new OpenAIRealtimeClient("key", "model");
        Assert.Throws<ArgumentNullException>("serviceType", () => client.GetService(null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        IRealtimeClient client = new OpenAIRealtimeClient("key", "model");
        client.Dispose();

        // Second dispose should not throw.
        client.Dispose();
        Assert.Null(client.GetService(typeof(string)));
    }

    [Fact]
    public async Task CreateSessionAsync_Cancelled_ReturnsNull()
    {
        using var client = new OpenAIRealtimeClient("key", "model");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var session = await client.CreateSessionAsync(cancellationToken: cts.Token);
        Assert.Null(session);
    }
}

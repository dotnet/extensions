// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () => ChatClientExtensions.GetService<object>(null!));
        Assert.Throws<ArgumentNullException>("client", () => ChatClientExtensions.GetRequiredService<object>(null!));
    }

    [Fact]
    public void GetService_ValidService_Returned()
    {
        using var client = new TestChatClient
        {
            GetServiceCallback = (Type serviceType, object? serviceKey) =>
            {
                if (serviceType == typeof(string))
                {
                    return serviceKey == null ? "null key" : "non-null key";
                }

                if (serviceType == typeof(IChatClient))
                {
                    return new object();
                }

                return null;
            },
        };

        Assert.Equal("null key", client.GetService<string>());
        Assert.Equal("null key", client.GetService<string>(null));
        Assert.Equal("non-null key", client.GetService<string>("key"));

        Assert.Null(client.GetService<object>());
        Assert.Null(client.GetService<object>("key"));
        Assert.Null(client.GetService<IChatClient>());

        Assert.Equal("null key", client.GetRequiredService<string>());
        Assert.Equal("null key", client.GetRequiredService<string>(null));
        Assert.Equal("non-null key", client.GetRequiredService<string>("key"));

        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService<object>());
        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService<object>("key"));
        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void GetResponseAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = ChatClientExtensions.GetResponseAsync(null!, "hello");
        });

        Assert.Throws<ArgumentNullException>("chatMessage", () =>
        {
            _ = ChatClientExtensions.GetResponseAsync(new TestChatClient(), (ChatMessage)null!);
        });
    }

    [Fact]
    public void GetStreamingResponseAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = ChatClientExtensions.GetStreamingResponseAsync(null!, "hello");
        });

        Assert.Throws<ArgumentNullException>("chatMessage", () =>
        {
            _ = ChatClientExtensions.GetStreamingResponseAsync(new TestChatClient(), (ChatMessage)null!);
        });
    }

    [Fact]
    public async Task GetResponseAsync_CreatesTextMessageAsync()
    {
        var expectedResponse = new ChatResponse([new ChatMessage()]);
        var expectedOptions = new ChatOptions();
        using var cts = new CancellationTokenSource();

        using TestChatClient client = new()
        {
            GetResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                ChatMessage m = Assert.Single(chatMessages);
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("hello", m.Text);

                Assert.Same(expectedOptions, options);

                Assert.Equal(cts.Token, cancellationToken);

                return Task.FromResult(expectedResponse);
            },
        };

        ChatResponse response = await client.GetResponseAsync("hello", expectedOptions, cts.Token);

        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_CreatesTextMessageAsync()
    {
        var expectedOptions = new ChatOptions();
        using var cts = new CancellationTokenSource();

        using TestChatClient client = new()
        {
            GetStreamingResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                ChatMessage m = Assert.Single(chatMessages);
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("hello", m.Text);

                Assert.Same(expectedOptions, options);

                Assert.Equal(cts.Token, cancellationToken);

                return YieldAsync([new ChatResponseUpdate { Text = "world" }]);
            },
        };

        int count = 0;
        await foreach (var update in client.GetStreamingResponseAsync("hello", expectedOptions, cts.Token))
        {
            Assert.Equal(0, count);
            Assert.Equal("world", update.Text);
            count++;
        }

        Assert.Equal(1, count);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(params ChatResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}

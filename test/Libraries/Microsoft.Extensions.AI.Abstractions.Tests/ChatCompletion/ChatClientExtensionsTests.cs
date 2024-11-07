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
    }

    [Fact]
    public void CompleteAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = ChatClientExtensions.CompleteAsync(null!, "hello");
        });

        Assert.Throws<ArgumentNullException>("chatMessage", () =>
        {
            _ = ChatClientExtensions.CompleteAsync(new TestChatClient(), null!);
        });
    }

    [Fact]
    public void CompleteStreamingAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = ChatClientExtensions.CompleteStreamingAsync(null!, "hello");
        });

        Assert.Throws<ArgumentNullException>("chatMessage", () =>
        {
            _ = ChatClientExtensions.CompleteStreamingAsync(new TestChatClient(), null!);
        });
    }

    [Fact]
    public async Task CompleteAsync_CreatesTextMessageAsync()
    {
        var expectedResponse = new ChatCompletion([new ChatMessage()]);
        var expectedOptions = new ChatOptions();
        using var cts = new CancellationTokenSource();

        using TestChatClient client = new()
        {
            CompleteAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                ChatMessage m = Assert.Single(chatMessages);
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("hello", m.Text);

                Assert.Same(expectedOptions, options);

                Assert.Equal(cts.Token, cancellationToken);

                return Task.FromResult(expectedResponse);
            },
        };

        ChatCompletion response = await client.CompleteAsync("hello", expectedOptions, cts.Token);

        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task CompleteStreamingAsync_CreatesTextMessageAsync()
    {
        var expectedOptions = new ChatOptions();
        using var cts = new CancellationTokenSource();

        using TestChatClient client = new()
        {
            CompleteStreamingAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                ChatMessage m = Assert.Single(chatMessages);
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("hello", m.Text);

                Assert.Same(expectedOptions, options);

                Assert.Equal(cts.Token, cancellationToken);

                return YieldAsync([new StreamingChatCompletionUpdate { Text = "world" }]);
            },
        };

        int count = 0;
        await foreach (var update in client.CompleteStreamingAsync("hello", expectedOptions, cts.Token))
        {
            Assert.Equal(0, count);
            Assert.Equal("world", update.Text);
            count++;
        }

        Assert.Equal(1, count);
    }

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> YieldAsync(params StreamingChatCompletionUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}

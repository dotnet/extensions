// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class UseDelegateChatClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        using var client = new TestChatClient();
        ChatClientBuilder builder = new(client);

        Assert.Throws<ArgumentNullException>("sharedFunc", () =>
            builder.Use((AnonymousDelegatingChatClient.GetResponseSharedFunc)null!));

        Assert.Throws<ArgumentNullException>("getResponseFunc", () => builder.Use(null!, null!));

        Assert.Throws<ArgumentNullException>("innerClient", () => new AnonymousDelegatingChatClient(null!, delegate { return Task.CompletedTask; }));
        Assert.Throws<ArgumentNullException>("sharedFunc", () => new AnonymousDelegatingChatClient(client, null!));

        Assert.Throws<ArgumentNullException>("innerClient", () => new AnonymousDelegatingChatClient(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("getResponseFunc", () => new AnonymousDelegatingChatClient(client, null!, null!));
    }

    [Fact]
    public async Task Shared_ContextPropagated()
    {
        IList<ChatMessage> expectedMessages = [];
        ChatOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();

        AsyncLocal<int> asyncLocal = new();

        using IChatClient innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "hello")));
            },

            GetStreamingResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new ChatResponseUpdate { Text = "world" });
            },
        };

        using IChatClient client = new ChatClientBuilder(innerClient)
            .Use(async (chatMessages, options, next, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                await next(chatMessages, options, cancellationToken);
            })
            .Build();

        Assert.Equal(0, asyncLocal.Value);
        ChatResponse response = await client.GetResponseAsync(expectedMessages, expectedOptions, expectedCts.Token);
        Assert.Equal("hello", response.Message.Text);

        Assert.Equal(0, asyncLocal.Value);
        response = await client.GetStreamingResponseAsync(expectedMessages, expectedOptions, expectedCts.Token).ToChatResponseAsync();
        Assert.Equal("world", response.Message.Text);
    }

    [Fact]
    public async Task GetResponseFunc_ContextPropagated()
    {
        IList<ChatMessage> expectedMessages = [];
        ChatOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using IChatClient innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "hello")));
            },
        };

        using IChatClient client = new ChatClientBuilder(innerClient)
            .Use(async (chatMessages, options, innerClient, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                var cc = await innerClient.GetResponseAsync(chatMessages, options, cancellationToken);
                cc.Choices[0].Text += " world";
                return cc;
            }, null)
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        ChatResponse response = await client.GetResponseAsync(expectedMessages, expectedOptions, expectedCts.Token);
        Assert.Equal("hello world", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedMessages, expectedOptions, expectedCts.Token).ToChatResponseAsync();
        Assert.Equal("hello world", response.Message.Text);
    }

    [Fact]
    public async Task GetStreamingResponseFunc_ContextPropagated()
    {
        IList<ChatMessage> expectedMessages = [];
        ChatOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using IChatClient innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new ChatResponseUpdate { Text = "hello" });
            },
        };

        using IChatClient client = new ChatClientBuilder(innerClient)
            .Use(null, (chatMessages, options, innerClient, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                return Impl(chatMessages, options, innerClient, cancellationToken);

                static async IAsyncEnumerable<ChatResponseUpdate> Impl(
                    IList<ChatMessage> chatMessages, ChatOptions? options, IChatClient innerClient, [EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    await foreach (var update in innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
                    {
                        yield return update;
                    }

                    yield return new() { Text = " world" };
                }
            })
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        ChatResponse response = await client.GetResponseAsync(expectedMessages, expectedOptions, expectedCts.Token);
        Assert.Equal("hello world", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedMessages, expectedOptions, expectedCts.Token).ToChatResponseAsync();
        Assert.Equal("hello world", response.Message.Text);
    }

    [Fact]
    public async Task BothGetResponseAndGetStreamingResponseFuncs_ContextPropagated()
    {
        IList<ChatMessage> expectedMessages = [];
        ChatOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using IChatClient innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "non-streaming hello")));
            },

            GetStreamingResponseAsyncCallback = (chatMessages, options, cancellationToken) =>
            {
                Assert.Same(expectedMessages, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new ChatResponseUpdate { Text = "streaming hello" });
            },
        };

        using IChatClient client = new ChatClientBuilder(innerClient)
            .Use(
                async (chatMessages, options, innerClient, cancellationToken) =>
                {
                    Assert.Same(expectedMessages, chatMessages);
                    Assert.Same(expectedOptions, options);
                    Assert.Equal(expectedCts.Token, cancellationToken);
                    asyncLocal.Value = 42;
                    var cc = await innerClient.GetResponseAsync(chatMessages, options, cancellationToken);
                    cc.Choices[0].Text += " world (non-streaming)";
                    return cc;
                },
                (chatMessages, options, innerClient, cancellationToken) =>
                {
                    Assert.Same(expectedMessages, chatMessages);
                    Assert.Same(expectedOptions, options);
                    Assert.Equal(expectedCts.Token, cancellationToken);
                    asyncLocal.Value = 42;
                    return Impl(chatMessages, options, innerClient, cancellationToken);

                    static async IAsyncEnumerable<ChatResponseUpdate> Impl(
                        IList<ChatMessage> chatMessages, ChatOptions? options, IChatClient innerClient, [EnumeratorCancellation] CancellationToken cancellationToken)
                    {
                        await foreach (var update in innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
                        {
                            yield return update;
                        }

                        yield return new() { Text = " world (streaming)" };
                    }
                })
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        ChatResponse response = await client.GetResponseAsync(expectedMessages, expectedOptions, expectedCts.Token);
        Assert.Equal("non-streaming hello world (non-streaming)", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedMessages, expectedOptions, expectedCts.Token).ToChatResponseAsync();
        Assert.Equal("streaming hello world (streaming)", response.Message.Text);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(params ChatResponseUpdate[] updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}

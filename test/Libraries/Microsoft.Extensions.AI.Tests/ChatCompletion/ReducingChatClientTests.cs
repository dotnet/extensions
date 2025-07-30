// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ReducingChatClientTests
{
    [Fact]
    public void ReducingChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ReducingChatClient(null!, new TestReducer()));
        Assert.Throws<ArgumentNullException>("reducer", () => new ReducingChatClient(new TestChatClient(), null!));
    }

    [Fact]
    public void UseChatReducer_InvalidArgs_Throws()
    {
        using var innerClient = new TestChatClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("builder", () => ReducingChatClientBuilderExtensions.UseChatReducer(null!, new TestReducer()));
        Assert.Throws<ArgumentNullException>("reducer", () => builder.UseChatReducer(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetResponseAsync_CallsReducerBeforeInnerClient(bool streaming)
    {
        var originalMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant"),
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi there!"),
            new(ChatRole.User, "What's the weather?")
        };

        var reducedMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant"),
            new(ChatRole.User, "What's the weather?")
        };

        var reducer = new TestReducer { ReducedMessages = reducedMessages };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "It's sunny!"));
        var expectedUpdates = new[] { new ChatResponseUpdate(ChatRole.Assistant, "It's"), new ChatResponseUpdate(null, " sunny!") };

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                // Verify that the inner client receives the reduced messages
                Assert.Same(reducedMessages, messages);
                return Task.FromResult(expectedResponse);
            },
            GetStreamingResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                // Verify that the inner client receives the reduced messages
                Assert.Same(reducedMessages, messages);
                return ToAsyncEnumerable(expectedUpdates);
            }
        };

        using var client = new ReducingChatClient(innerClient, reducer);

        if (!streaming)
        {
            var response = await client.GetResponseAsync(originalMessages);
            Assert.Same(expectedResponse, response);
        }
        else
        {
            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in client.GetStreamingResponseAsync(originalMessages))
            {
                updates.Add(update);
            }

            Assert.Equal(expectedUpdates.Length, updates.Count);
            for (int i = 0; i < expectedUpdates.Length; i++)
            {
                Assert.Same(expectedUpdates[i], updates[i]);
            }
        }

        Assert.Equal(1, reducer.ReduceAsyncCallCount);
        Assert.Same(originalMessages, reducer.LastMessagesProvided);
    }

    [Fact]
    public async Task GetResponseAsync_PassesThroughOptionsAndCancellationToken()
    {
        var reducer = new TestReducer { ReducedMessages = new List<ChatMessage>() };
        var options = new ChatOptions { Temperature = 0.7f };
        using var cts = new CancellationTokenSource();

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, opts, cancellationToken) =>
            {
                Assert.Same(options, opts);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(new ChatResponse());
            }
        };

        using var client = new ReducingChatClient(innerClient, reducer);
        await client.GetResponseAsync(Array.Empty<ChatMessage>(), options, cts.Token);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_PassesThroughOptionsAndCancellationToken()
    {
        var reducer = new TestReducer { ReducedMessages = new List<ChatMessage>() };
        var options = new ChatOptions { Temperature = 0.7f };
        using var cts = new CancellationTokenSource();

        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (messages, opts, cancellationToken) =>
            {
                Assert.Same(options, opts);
                Assert.Equal(cts.Token, cancellationToken);
                return ToAsyncEnumerable(Array.Empty<ChatResponseUpdate>());
            }
        };

        using var client = new ReducingChatClient(innerClient, reducer);
        await foreach (var _ in client.GetStreamingResponseAsync(Array.Empty<ChatMessage>(), options, cts.Token))
        {
            // Just iterate to trigger the callback
        }
    }

    [Fact]
    public async Task ReducerException_PropagatesFromGetResponseAsync()
    {
        var reducer = new TestReducer { ThrowOnReduce = true };
        using var innerClient = new TestChatClient();
        using var client = new ReducingChatClient(innerClient, reducer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetResponseAsync(new List<ChatMessage>()));
    }

    [Fact]
    public async Task ReducerException_PropagatesFromGetStreamingResponseAsync()
    {
        var reducer = new TestReducer { ThrowOnReduce = true };
        using var innerClient = new TestChatClient();
        using var client = new ReducingChatClient(innerClient, reducer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ConsumeStreamAsync(client.GetStreamingResponseAsync(new List<ChatMessage>())));
    }

    [Fact]
    public async Task UseChatReducer_IntegratesWithBuilder()
    {
        var originalMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Message 1"),
            new(ChatRole.Assistant, "Response 1"),
            new(ChatRole.User, "Message 2")
        };

        var reducedMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Message 2")
        };

        var reducer = new TestReducer { ReducedMessages = reducedMessages };

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Same(reducedMessages, messages);
                return Task.FromResult(new ChatResponse());
            }
        };

        using var client = innerClient
            .AsBuilder()
            .UseChatReducer(reducer)
            .Build();

        await client.GetResponseAsync(originalMessages);
        Assert.Equal(1, reducer.ReduceAsyncCallCount);
    }

    [Fact]
    public async Task ReducerCancellationToken_PropagatedToReducer()
    {
        using var cts = new CancellationTokenSource();
        var reducer = new TestReducer
        {
            ReduceAsyncCallback = (messages, cancellationToken) =>
            {
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult<IList<ChatMessage>>(messages.ToList());
            }
        };

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
                Task.FromResult(new ChatResponse())
        };

        using var client = new ReducingChatClient(innerClient, reducer);
        await client.GetResponseAsync(new List<ChatMessage>(), null, cts.Token);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async Task ConsumeStreamAsync(IAsyncEnumerable<ChatResponseUpdate> stream)
    {
        await foreach (var _ in stream)
        {
            // Just consume the stream
        }
    }

    private sealed class TestReducer : IChatReducer
    {
        public IList<ChatMessage>? ReducedMessages { get; set; }
        public bool ThrowOnReduce { get; set; }
        public int ReduceAsyncCallCount { get; private set; }
        public IEnumerable<ChatMessage>? LastMessagesProvided { get; private set; }
        public Func<IEnumerable<ChatMessage>, CancellationToken, Task<IList<ChatMessage>>>? ReduceAsyncCallback { get; set; }

        public Task<IList<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            ReduceAsyncCallCount++;
            LastMessagesProvided = messages;

            if (ThrowOnReduce)
            {
                throw new InvalidOperationException("Test exception from reducer");
            }

            if (ReduceAsyncCallback is not null)
            {
                return ReduceAsyncCallback(messages, cancellationToken);
            }

            return Task.FromResult(ReducedMessages ?? messages.ToList());
        }
    }
}

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

        if (streaming)
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
        else
        {
            var response = await client.GetResponseAsync(originalMessages);
            Assert.Same(expectedResponse, response);
        }

        Assert.Equal(1, reducer.ReduceAsyncCallCount);
        Assert.Same(originalMessages, reducer.LastMessagesProvided);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private sealed class TestReducer : IChatReducer
    {
        public IList<ChatMessage>? ReducedMessages { get; set; }
        public int ReduceAsyncCallCount { get; private set; }
        public IEnumerable<ChatMessage>? LastMessagesProvided { get; private set; }

        public Task<IList<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            ReduceAsyncCallCount++;
            LastMessagesProvided = messages;
            return Task.FromResult(ReducedMessages ?? messages.ToList());
        }
    }
}

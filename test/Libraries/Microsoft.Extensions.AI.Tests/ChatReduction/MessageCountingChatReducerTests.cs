// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class MessageCountingChatReducerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_ThrowsOnInvalidTargetCount(int targetCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>("targetCount", () => new MessageCountingChatReducer(targetCount));
    }

    [Fact]
    public void Constructor_AcceptsValidTargetCount()
    {
        var reducer = new MessageCountingChatReducer(5);
        Assert.NotNull(reducer);
    }

    [Fact]
    public async Task ReduceAsync_ThrowsOnNullMessages()
    {
        var reducer = new MessageCountingChatReducer(5);
        await Assert.ThrowsAsync<ArgumentNullException>(() => reducer.ReduceAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReduceAsync_HandlesEmptyMessages()
    {
        var reducer = new MessageCountingChatReducer(5);
        var result = await reducer.ReduceAsync([], CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReduceAsync_PreservesFirstSystemMessage()
    {
        var reducer = new MessageCountingChatReducer(2);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there!"),
            new ChatMessage(ChatRole.User, "How are you?"),
            new ChatMessage(ChatRole.Assistant, "I'm doing well, thanks!"),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Collection(resultList,
            m =>
            {
                Assert.Equal(ChatRole.System, m.Role);
                Assert.Equal("You are a helpful assistant.", m.Text);
            },
            m =>
            {
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("How are you?", m.Text);
            },
            m =>
            {
                Assert.Equal(ChatRole.Assistant, m.Role);
                Assert.Equal("I'm doing well, thanks!", m.Text);
            });
    }

    [Fact]
    public async Task ReduceAsync_OnlyFirstSystemMessageIsPreserved()
    {
        var reducer = new MessageCountingChatReducer(2);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "First system message"),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.System, "Second system message"),
            new ChatMessage(ChatRole.Assistant, "Hi"),
            new ChatMessage(ChatRole.User, "How are you?"),
            new ChatMessage(ChatRole.Assistant, "I'm fine!"),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Collection(resultList,
            m =>
            {
                Assert.Equal(ChatRole.System, m.Role);
                Assert.Equal("First system message", m.Text);
            },
            m =>
            {
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("How are you?", m.Text);
            },
            m =>
            {
                Assert.Equal(ChatRole.Assistant, m.Role);
                Assert.Equal("I'm fine!", m.Text);
            });

        // Second system message should not be preserved separately
        Assert.Equal(1, resultList.Count(m => m.Role == ChatRole.System));
    }

    [Fact]
    public async Task ReduceAsync_IgnoresFunctionCallsAndResults()
    {
        var reducer = new MessageCountingChatReducer(2);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "What's the weather?"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call1", "get_weather", new Dictionary<string, object?> { ["location"] = "Seattle" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "Sunny, 72°F")]),
            new ChatMessage(ChatRole.Assistant, "The weather in Seattle is sunny and 72°F."),
            new ChatMessage(ChatRole.User, "Thanks!"),
            new ChatMessage(ChatRole.Assistant, "You're welcome!"),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Collection(resultList,
            m =>
            {
                Assert.Equal(ChatRole.User, m.Role);
                Assert.Equal("Thanks!", m.Text);
                Assert.DoesNotContain(m.Contents, c => c is FunctionCallContent);
                Assert.DoesNotContain(m.Contents, c => c is FunctionResultContent);
            },
            m =>
            {
                Assert.Equal(ChatRole.Assistant, m.Role);
                Assert.Equal("You're welcome!", m.Text);
                Assert.DoesNotContain(m.Contents, c => c is FunctionCallContent);
                Assert.DoesNotContain(m.Contents, c => c is FunctionResultContent);
            });
    }

    [Theory]
    [InlineData(5, 3, 3)]  // Less messages than target
    [InlineData(5, 5, 5)]  // Exactly at target
    [InlineData(5, 8, 5)]  // More messages than target
    [InlineData(1, 10, 1)] // Only keep 1 message
    public async Task ReduceAsync_RespectsTargetCount(int targetCount, int messageCount, int expectedCount)
    {
        var reducer = new MessageCountingChatReducer(targetCount);

        var messages = new List<ChatMessage>();
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(new ChatMessage(i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"Message {i}"));
        }

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Equal(expectedCount, resultList.Count);

        // Verify we kept the most recent messages
        if (messageCount > targetCount)
        {
            var startIndex = messageCount - targetCount;
            var expectedMessages = new Action<ChatMessage>[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                var expectedIndex = startIndex + i;
                var expectedRole = expectedIndex % 2 == 0 ? ChatRole.User : ChatRole.Assistant;
                expectedMessages[i] = m =>
                {
                    Assert.Equal(expectedRole, m.Role);
                    Assert.Equal($"Message {expectedIndex}", m.Text);
                };
            }

            Assert.Collection(resultList, expectedMessages);
        }
    }

    [Fact]
    public async Task ReduceAsync_HandlesOnlySystemMessage()
    {
        var reducer = new MessageCountingChatReducer(5);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "System prompt"),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Collection(resultList,
            m =>
            {
                Assert.Equal(ChatRole.System, m.Role);
                Assert.Equal("System prompt", m.Text);
            });
    }

    [Fact]
    public async Task ReduceAsync_HandlesOnlyFunctionMessages()
    {
        var reducer = new MessageCountingChatReducer(5);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call1", "func", null)]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "result")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call2", "func", null)]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call2", "result")]),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReduceAsync_HandlesTargetCountOfOne()
    {
        var reducer = new MessageCountingChatReducer(1);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "System"),
            new ChatMessage(ChatRole.User, "First"),
            new ChatMessage(ChatRole.Assistant, "Second"),
            new ChatMessage(ChatRole.User, "Third"),
            new ChatMessage(ChatRole.Assistant, "Fourth"),
        ];

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        Assert.Collection(resultList,
            m =>
            {
                Assert.Equal(ChatRole.System, m.Role);
                Assert.Equal("System", m.Text);
            },
            m =>
            {
                Assert.Equal(ChatRole.Assistant, m.Role);
                Assert.Equal("Fourth", m.Text);
            });
    }
}

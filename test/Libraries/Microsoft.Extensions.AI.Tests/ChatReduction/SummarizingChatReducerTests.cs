// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S103 // Lines should not be too long

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SummarizingChatReducerTests
{
    [Fact]
    public void Constructor_ThrowsOnNullChatClient()
    {
        Assert.Throws<ArgumentNullException>("chatClient", () => new SummarizingChatReducer(null!, targetCount: 5, threshold: 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_ThrowsOnInvalidTargetCount(int targetCount)
    {
        using var chatClient = new TestChatClient();
        Assert.Throws<ArgumentOutOfRangeException>(nameof(targetCount), () => new SummarizingChatReducer(chatClient, targetCount, threshold: 2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_ThrowsOnInvalidThresholdCount(int thresholdCount)
    {
        using var chatClient = new TestChatClient();
        Assert.Throws<ArgumentOutOfRangeException>("threshold", () => new SummarizingChatReducer(chatClient, targetCount: 5, thresholdCount));
    }

    [Fact]
    public async Task ReduceAsync_ThrowsOnNullMessages()
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 5, threshold: 2);
        await Assert.ThrowsAsync<ArgumentNullException>(() => reducer.ReduceAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReduceAsync_HandlesEmptyMessages()
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 5, threshold: 2);

        var result = await reducer.ReduceAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReduceAsync_PreservesSystemMessage()
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 1, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there!"),
            new ChatMessage(ChatRole.User, "How are you?"),
        ];

        chatClient.GetResponseAsyncCallback = (_, _, _) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Summary of conversation")));

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count); // System + Summary + 1 unsummarized
        Assert.Equal(ChatRole.System, resultList[0].Role);
        Assert.Equal("You are a helpful assistant.", resultList[0].Text);
    }

    [Fact]
    public async Task ReduceAsync_PreservesCompleteToolCallSequence()
    {
        using var chatClient = new TestChatClient();

        // Target 2 messages, but this would split a function call sequence
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 2, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "What's the time?"),
            new ChatMessage(ChatRole.Assistant, "Let me check"),
            new ChatMessage(ChatRole.User, "What's the weather?"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call1", "get_weather"), new TestInputRequestContent("uir1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "Sunny")]),
            new ChatMessage(ChatRole.User, [new TestInputResponseContent("uir1")]),
            new ChatMessage(ChatRole.Assistant, "It's sunny"),
        ];

        chatClient.GetResponseAsyncCallback = (msgs, _, _) =>
        {
            Assert.DoesNotContain(msgs, m => m.Contents.Any(c => c is FunctionCallContent or FunctionResultContent or TestInputRequestContent or TestInputResponseContent));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Asked about time")));
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        // Should have: summary + function call + function result + user input response + last reply
        Assert.Equal(5, resultList.Count);

        // Verify the complete sequence is preserved
        Assert.Collection(resultList,
            m => Assert.Contains("Asked about time", m.Text),
            m =>
            {
                Assert.Contains(m.Contents, c => c is FunctionCallContent);
                Assert.Contains(m.Contents, c => c is TestInputRequestContent);
            },
            m => Assert.Contains(m.Contents, c => c is FunctionResultContent),
            m => Assert.Contains(m.Contents, c => c is TestInputResponseContent),
            m => Assert.Contains("sunny", m.Text));
    }

    [Fact]
    public async Task ReduceAsync_PreservesUserMessageWhenWithinThreshold()
    {
        using var chatClient = new TestChatClient();

        // Target 3 messages with threshold of 2
        // This allows us to keep anywhere from 3 to 5 messages
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 3, threshold: 2);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "First question"),
            new ChatMessage(ChatRole.Assistant, "First answer"),
            new ChatMessage(ChatRole.User, "Second question"),
            new ChatMessage(ChatRole.Assistant, "Second answer"),
            new ChatMessage(ChatRole.User, "Third question"),
            new ChatMessage(ChatRole.Assistant, "Third answer"),
        ];

        chatClient.GetResponseAsyncCallback = (msgs, _, _) =>
        {
            var msgList = msgs.ToList();

            // Should summarize messages 0-1 (First question and answer)
            // The reducer should find the User message at index 2 within the threshold
            Assert.Equal(3, msgList.Count); // 2 messages to summarize + system prompt
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Summary of first exchange")));
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        // Should have: summary + 4 kept messages (from "Second question" onward)
        Assert.Equal(5, resultList.Count);

        // Verify the summary is first
        Assert.Contains("Summary", resultList[0].Text);

        // Verify we kept the User message at index 2 and everything after
        Assert.Collection(resultList.Skip(1),
            m => Assert.Contains("Second question", m.Text),
            m => Assert.Contains("Second answer", m.Text),
            m => Assert.Contains("Third question", m.Text),
            m => Assert.Contains("Third answer", m.Text));
    }

    [Fact]
    public async Task ReduceAsync_ExcludesToolCallsFromSummarizedPortion()
    {
        using var chatClient = new TestChatClient();

        // Target 3 messages - this will cause function calls in older messages to be summarized (excluded)
        // while function calls in recent messages are kept
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 3, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "What's the weather in Seattle?"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call1", "get_weather", new Dictionary<string, object?> { ["location"] = "Seattle" }), new TestInputRequestContent("uir2")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "Sunny, 72°F")]),
            new ChatMessage(ChatRole.User, [new TestInputResponseContent("uir2")]),
            new ChatMessage(ChatRole.Assistant, "It's sunny and 72°F in Seattle."),
            new ChatMessage(ChatRole.User, "What about New York?"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call2", "get_weather", new Dictionary<string, object?> { ["location"] = "New York" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call2", "Rainy, 65°F")]),
            new ChatMessage(ChatRole.Assistant, "It's rainy and 65°F in New York."),
        ];

        chatClient.GetResponseAsyncCallback = (msgs, _, _) =>
        {
            var msgList = msgs.ToList();

            Assert.Equal(4, msgList.Count); // 3 non-function messages + system prompt
            Assert.DoesNotContain(msgList, m => m.Contents.Any(c => c is FunctionCallContent or FunctionResultContent or TestInputRequestContent or TestInputResponseContent));
            Assert.Contains(msgList, m => m.Text.Contains("What's the weather in Seattle?"));
            Assert.Contains(msgList, m => m.Text.Contains("sunny and 72°F in Seattle"));
            Assert.Contains(msgList, m => m.Text.Contains("What about New York?"));
            Assert.Contains(msgList, m => m.Role == ChatRole.System);

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "User asked about weather in Seattle and New York.")));
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();

        // Should have: summary + 3 kept messages (the last 3 messages with function calls)
        Assert.Equal(4, resultList.Count);

        Assert.Contains("User asked about weather", resultList[0].Text);
        Assert.Contains(resultList, m => m.Contents.Any(c => c is FunctionCallContent fc && fc.CallId == "call2"));
        Assert.Contains(resultList, m => m.Contents.Any(c => c is FunctionResultContent fr && fr.CallId == "call2"));
        Assert.DoesNotContain(resultList, m => m.Contents.Any(c => c is FunctionCallContent fc && fc.CallId == "call1"));
        Assert.DoesNotContain(resultList, m => m.Contents.Any(c => c is FunctionResultContent fr && fr.CallId == "call1"));
        Assert.DoesNotContain(resultList, m => m.Contents.Any(c => c is TestInputRequestContent));
        Assert.DoesNotContain(resultList, m => m.Contents.Any(c => c is TestInputResponseContent));
        Assert.DoesNotContain(resultList, m => m.Text.Contains("sunny and 72°F in Seattle"));
    }

    [Theory]
    [InlineData(5, 0, 5, false)]  // Exactly at target, no summarization
    [InlineData(5, 0, 4, false)]  // Below target, no summarization
    [InlineData(5, 0, 6, true)]  // Above target by 1, triggers summarization
    [InlineData(5, 2, 7, false)]  // At threshold boundary, no summarization
    [InlineData(5, 2, 8, true)]  // Above threshold, triggers summarization
    public async Task ReduceAsync_RespectsTargetAndThresholdCounts(int targetCount, int thresholdCount, int messageCount, bool shouldSummarize)
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount, thresholdCount);

        var messages = new List<ChatMessage>();
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(new ChatMessage(ChatRole.Assistant, $"Message {i}"));
        }

        var summarizationCalled = false;
        chatClient.GetResponseAsyncCallback = (_, _, _) =>
        {
            summarizationCalled = true;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Summary")));
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.Equal(shouldSummarize, summarizationCalled);

        if (shouldSummarize)
        {
            var resultList = result.ToList();
            Assert.Equal(targetCount + 1, resultList.Count); // Summary + target messages
            Assert.StartsWith("Summary", resultList[0].Text, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal(messageCount, result.Count());
        }
    }

    [Fact]
    public async Task ReduceAsync_CancellationTokenIsRespected()
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 1, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "Message 1"),
            new ChatMessage(ChatRole.Assistant, "Response 1"),
            new ChatMessage(ChatRole.User, "Message 2"),
        ];

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        chatClient.GetResponseAsyncCallback = (_, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Summary")));
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reducer.ReduceAsync(messages, cts.Token));
    }

    [Fact]
    public async Task ReduceAsync_OnlyFirstSystemMessageIsPreserved()
    {
        using var chatClient = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClient, targetCount: 1, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "First system message"),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.System, "Second system message"),
            new ChatMessage(ChatRole.Assistant, "Hi"),
            new ChatMessage(ChatRole.User, "How are you?"),
        ];

        chatClient.GetResponseAsyncCallback = (_, _, _) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Summary")));

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Equal(ChatRole.System, resultList[0].Role);
        Assert.Equal("First system message", resultList[0].Text);

        // Second system message should not be preserved separately
        Assert.Equal(1, resultList.Count(m => m.Role == ChatRole.System));
    }

    [Fact]
    public async Task CanHaveSummarizedConversation()
    {
        using var chatClientForSummarization = new TestChatClient();
        var reducer = new SummarizingChatReducer(chatClientForSummarization, targetCount: 2, threshold: 0);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "Hi there! Can you tell me about golden retrievers?"),
            new ChatMessage(ChatRole.Assistant, "Of course! Golden retrievers are known for their friendly and tolerant attitudes. They're great family pets and are very intelligent and easy to train."),
            new ChatMessage(ChatRole.User, "What kind of exercise do they need?"),
            new ChatMessage(ChatRole.Assistant, "Golden retrievers are quite active and need regular exercise. Daily walks, playtime, and activities like fetching or swimming are great for them."),
            new ChatMessage(ChatRole.User, "Are they good with kids?"),
        ];

        chatClientForSummarization.GetResponseAsyncCallback = (messages, options, cancellationToken) =>
        {
            Assert.Equal(4, messages.Count()); // 3 messages to summarize + 1 system prompt
            Assert.Collection(messages,
                m => Assert.StartsWith("Hi there!", m.Text, StringComparison.Ordinal),
                m => Assert.StartsWith("Of course!", m.Text, StringComparison.Ordinal),
                m => Assert.StartsWith("What kind of exercise", m.Text, StringComparison.Ordinal),
                m => Assert.Equal(ChatRole.System, m.Role));
            const string Summary = """
                The user asked for information about golden retrievers.
                The assistant explained that they have characteristics making them great family pets.
                The user then asked what kind of exercise they need.
                """;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Summary)));
        };

        var reducedMessages = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.Equal(3, reducedMessages.Count()); // 1 summary + 2 unsummarized messages
        Assert.Collection(reducedMessages,
            m => Assert.StartsWith("The user asked for information", m.Text, StringComparison.Ordinal),
            m => Assert.StartsWith("Golden retrievers are quite", m.Text, StringComparison.Ordinal),
            m => Assert.StartsWith("Are they good with kids", m.Text, StringComparison.Ordinal));

        messages.Add(new ChatMessage(ChatRole.Assistant, "Golden retrievers get along well with kids! They're able to be playful and energetic while remaining gentle."));
        messages.Add(new ChatMessage(ChatRole.User, "Do they make good lap dogs?"));

        chatClientForSummarization.GetResponseAsyncCallback = (messages, options, cancellationToken) =>
        {
            Assert.Equal(4, messages.Count()); // 1 summary message, 2 unsummarized message, 1 system prompt
            Assert.Collection(messages,
                m => Assert.StartsWith("The user asked", m.Text, StringComparison.Ordinal),
                m => Assert.StartsWith("Golden retrievers are quite active", m.Text, StringComparison.Ordinal),
                m => Assert.StartsWith("Are they good with kids", m.Text, StringComparison.Ordinal),
                m => Assert.Equal(ChatRole.System, m.Role));
            const string Summary = """
                The user and assistant are discussing characteristics of golden retrievers.
                The user asked what kind of exercise they need, and the assitant explained that golden retrievers
                need frequent exercise. The user then asked about whether they're good around kids.
                """;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Summary)));
        };

        reducedMessages = await reducer.ReduceAsync(messages, CancellationToken.None);
        Assert.Equal(3, reducedMessages.Count()); // 1 summary + 2 unsummarized messages
        Assert.Collection(reducedMessages,
            m => Assert.StartsWith("The user and assistant are discussing", m.Text, StringComparison.Ordinal),
            m => Assert.StartsWith("Golden retrievers get along", m.Text, StringComparison.Ordinal),
            m => Assert.StartsWith("Do they make good lap dogs", m.Text, StringComparison.Ordinal));
    }

    private sealed class TestInputRequestContent : InputRequestContent
    {
        public TestInputRequestContent(string requestId)
            : base(requestId)
        {
        }
    }

    private sealed class TestInputResponseContent : InputResponseContent
    {
        public TestInputResponseContent(string requestId)
            : base(requestId)
        {
        }
    }
}

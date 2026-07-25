// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class MockChatClientTests
{
    [Fact]
    public async Task AddResponse_MatchesMostRecentlyAddedSeed_AndFallsBackAfterSingleUseSeed()
    {
        using var client = new MockChatClient();
        client
            .AddResponse(static _ => true, static (_, _) => CreateResponseAsync("first"), singleUse: true)
            .AddResponse(static _ => true, static (_, _) => CreateResponseAsync("second"), singleUse: true);

        Assert.Equal("second", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("first", (await client.GetResponseAsync([new(ChatRole.User, "hello again")])).Text);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "third request")]));

        Assert.Contains("third request", ex.Message);
    }

    [Fact]
    public async Task AddResponse_IsReusableByDefault()
    {
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (_, _) => CreateResponseAsync("stable"));

        Assert.Equal("stable", (await client.GetResponseAsync([new(ChatRole.User, "first")])).Text);
        Assert.Equal("stable", (await client.GetResponseAsync([new(ChatRole.User, "second")])).Text);
    }

    [Fact]
    public async Task AddResponse_FactoryCanInspectRequestAndCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken observedToken = default;
        using var client = new MockChatClient();
        client.AddResponse(
            static request => request.LastUserText?.Contains("weather", StringComparison.OrdinalIgnoreCase) is true,
            (request, cancellationToken) =>
            {
                observedToken = cancellationToken;
                return CreateResponseAsync($"{request.LastUserText} branch");
            });

        ChatResponse response = await client.GetResponseAsync(
            [new(ChatRole.User, "What's the weather today?")],
            cancellationToken: cancellationTokenSource.Token);

        Assert.Equal("What's the weather today? branch", response.Text);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
    }

    [Fact]
    public async Task AddResponse_FactoryCanStageFunctionCallsUsingFunctionResults()
    {
        const string LoadDocumentsCallId = "load-documents";
        const string SearchCallId = "search";
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (request, _) => Task.FromResult(CreateToolResponse(request)));

        ChatMessage userMessage = new(ChatRole.User, "What does TrailMaster track?");

        ChatResponse loadDocuments = await client.GetResponseAsync([userMessage]);
        FunctionCallContent loadDocumentsCall = Assert.IsType<FunctionCallContent>(
            Assert.Single(Assert.Single(loadDocuments.Messages).Contents));
        Assert.Equal("LoadDocuments", loadDocumentsCall.Name);

        var loadDocumentsResult = new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(LoadDocumentsCallId, "Success: Function completed.")]);
        ChatResponse search = await client.GetResponseAsync([userMessage, loadDocuments.Messages[0], loadDocumentsResult]);
        FunctionCallContent searchCall = Assert.IsType<FunctionCallContent>(
            Assert.Single(Assert.Single(search.Messages).Contents));
        Assert.Equal("Search", searchCall.Name);
        Assert.Equal(userMessage.Text, searchCall.Arguments!["searchPhrase"]);

        var searchResult = new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(SearchCallId, new[] { "<result filename=\"Example_GPS_Watch.md\">distance traveled speed elevation gain heart rate</result>" })]);
        ChatResponse final = await client.GetResponseAsync(
            [userMessage, loadDocuments.Messages[0], loadDocumentsResult, search.Messages[0], searchResult]);

        Assert.Equal("Final answer", final.Text);

        static ChatResponse CreateToolResponse(MockChatClientRequest request)
        {
            if (HasFunctionResult(request, SearchCallId))
            {
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Final answer"));
            }

            if (HasFunctionResult(request, LoadDocumentsCallId))
            {
                return new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        [new FunctionCallContent(
                            SearchCallId,
                            "Search",
                            new Dictionary<string, object?>
                            {
                                ["searchPhrase"] = request.LastUserText,
                            })]));
            }

            return new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    [new FunctionCallContent(LoadDocumentsCallId, "LoadDocuments")]));
        }

        static bool HasFunctionResult(MockChatClientRequest request, string callId) =>
            request.Messages
                .SelectMany(static message => message.Contents)
                .OfType<FunctionResultContent>()
                .Any(result => result.CallId == callId);
    }

    [Fact]
    public async Task AddResponse_PropagatesFactoryCancellation()
    {
        using var client = new MockChatClient();
        client.AddResponse(
            static _ => true,
            static async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "unreachable"));
            });
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "cancel")], cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task AddStreamingResponse_SupportsNonStreamingConversion_WithCitationsReasoningAndUsage()
    {
        var text = new TextContent("Final answer")
        {
            Annotations =
            [
                new CitationAnnotation
                {
                    Title = "Example_GPS_Watch.md",
                    Snippet = "track your progress",
                }
            ]
        };

        ChatResponseUpdate[] updates =
        [
            new(ChatRole.Assistant, [text, new TextReasoningContent("Reasoning step")])
            {
                ConversationId = "conversation-1",
                ResponseId = "response-1",
                ModelId = "mock-model",
                FinishReason = ChatFinishReason.Stop,
            },
            new()
            {
                Contents = [new UsageContent(new UsageDetails { InputTokenCount = 3, OutputTokenCount = 7 })]
            }
        ];

        using var client = new MockChatClient();
        client.AddStreamingResponse(
            static _ => true,
            (_, _) => EnumerateUpdatesAsync(updates));

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "question")]);
        ChatMessage message = Assert.Single(response.Messages);

        var textContent = Assert.IsType<TextContent>(message.Contents[0]);
        Assert.Equal("Final answer", textContent.Text);
        var annotation = Assert.IsType<CitationAnnotation>(Assert.Single(textContent.Annotations!));
        Assert.Equal("Example_GPS_Watch.md", annotation.Title);
        Assert.Equal("track your progress", annotation.Snippet);

        Assert.Equal("Reasoning step", Assert.IsType<TextReasoningContent>(message.Contents[1]).Text);
        Assert.Equal("conversation-1", response.ConversationId);
        Assert.Equal("response-1", response.ResponseId);
        Assert.Equal("mock-model", response.ModelId);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);
        Assert.Equal(3, response.Usage!.InputTokenCount);
        Assert.Equal(7, response.Usage.OutputTokenCount);
    }

    [Fact]
    public async Task AddResponse_SupportsStreamingConversion()
    {
        var text = new TextContent("Streaming text")
        {
            Annotations =
            [
                new CitationAnnotation
                {
                    Title = "Example_Emergency_Survival_Kit.pdf",
                    Snippet = "water purification tablets",
                }
            ]
        };

        var response = new ChatResponse(
            [
                new ChatMessage(ChatRole.Assistant, [text, new TextReasoningContent("Think")])
            ])
        {
            Usage = new UsageDetails { OutputTokenCount = 5 },
            ConversationId = "conversation-2",
        };

        using var client = new MockChatClient();
        client.AddResponse(
            static _ => true,
            (_, _) => Task.FromResult(response));

        List<ChatResponseUpdate> updates = await ToListAsync(
            client.GetStreamingResponseAsync([new(ChatRole.User, "stream this")]));

        Assert.Equal(2, updates.Count); // message + usage
        Assert.Equal("conversation-2", updates[0].ConversationId);
        Assert.Equal("Streaming text", updates[0].Text);

        var streamedText = Assert.IsType<TextContent>(updates[0].Contents[0]);
        Assert.Equal("Streaming text", streamedText.Text);
        Assert.Equal("Example_Emergency_Survival_Kit.pdf", Assert.IsType<CitationAnnotation>(Assert.Single(streamedText.Annotations!)).Title);
        Assert.Equal("Think", Assert.IsType<TextReasoningContent>(updates[0].Contents[1]).Text);

        Assert.IsType<UsageContent>(Assert.Single(updates[1].Contents));
    }

    [Fact]
    public async Task AddResponse_UsesDistinctStreamingFactory()
    {
        using var client = new MockChatClient();
        client.AddResponse(
            static _ => true,
            static (_, _) => CreateResponseAsync("non-streaming"),
            static (_, _) => EnumerateUpdatesAsync([new(ChatRole.Assistant, "streaming")]));

        Assert.Equal("non-streaming", (await client.GetResponseAsync([new(ChatRole.User, "one")])).Text);

        List<ChatResponseUpdate> updates = await ToListAsync(
            client.GetStreamingResponseAsync([new(ChatRole.User, "two")]));
        Assert.Equal("streaming", Assert.Single(updates).Text);
    }

    [Fact]
    public async Task Requests_HistoryCapturesStreamingFlag()
    {
        using var client = new MockChatClient();
        client
            .AddResponse(static _ => true, static (_, _) => CreateResponseAsync("non-streaming"), singleUse: true)
            .AddResponse(static _ => true, static (_, _) => CreateResponseAsync("streaming"), singleUse: true);

        _ = await client.GetResponseAsync([new(ChatRole.User, "one")]);
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync([new(ChatRole.User, "two")]))
        {
            Assert.NotNull(update);
        }

        Assert.Equal(2, client.Requests.Count);
        Assert.False(client.Requests[0].IsStreaming);
        Assert.True(client.Requests[1].IsStreaming);
        Assert.Equal("one", client.Requests[0].LastUserText);
        Assert.Equal("two", client.Requests[1].LastUserText);
    }

    [Fact]
    public void GetService_ReturnsSelfWhenRequested()
    {
        using var client = new MockChatClient();

        Assert.Same(client, client.GetService(typeof(MockChatClient)));
        Assert.Same(client, client.GetService(typeof(IChatClient)));
        Assert.Null(client.GetService(typeof(string)));
    }

    [Fact]
    public async Task Dispose_PreventsFurtherUse()
    {
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (_, _) => CreateResponseAsync("before dispose"));

        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "after dispose")]));
    }

    [Fact]
    public async Task ClearResponses_RemovesSeedsAndPreservesRequests()
    {
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (_, _) => CreateResponseAsync("first"));

        _ = await client.GetResponseAsync([new(ChatRole.User, "first request")]);
        Assert.Same(client, client.ClearResponses());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "second request")]));

        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task AddException_UsesFreshExceptionForEachRequest()
    {
        using var client = new MockChatClient();
        client.AddException(
            static request => request.LastUserText == "fail",
            static () => new InvalidOperationException("expected"));

        InvalidOperationException first = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "fail")]));
        InvalidOperationException second = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "fail")]));

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task AddException_StreamingRequestFaultsDuringEnumeration()
    {
        using var client = new MockChatClient();
        client.AddException(static _ => true, static () => new InvalidOperationException("expected"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync([new(ChatRole.User, "fail")]))
            {
                Assert.NotNull(update);
            }
        });
    }

    [Fact]
    public void Registration_RequiresRequestPredicate()
    {
        using var client = new MockChatClient();

        Assert.Throws<ArgumentNullException>(() =>
            client.AddResponse(null!, static (_, _) => CreateResponseAsync("response")));
        Assert.Throws<ArgumentNullException>(() =>
            client.AddStreamingResponse(null!, static (_, _) => EnumerateUpdatesAsync([])));
        Assert.Throws<ArgumentNullException>(() =>
            client.AddException(null!, static () => new InvalidOperationException()));
    }

    private static Task<ChatResponse> CreateResponseAsync(string text) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    private static async IAsyncEnumerable<ChatResponseUpdate> EnumerateUpdatesAsync(IEnumerable<ChatResponseUpdate> updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static async Task<List<ChatResponseUpdate>> ToListAsync(IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        var results = new List<ChatResponseUpdate>();
        await foreach (ChatResponseUpdate update in updates)
        {
            results.Add(update);
        }

        return results;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    public async Task AddResponses_UsesOrdinalIgnoreCaseExactMatches()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["hello"] = "Hello from a deterministic mock.",
                ["goodbye"] = "Goodbye from a deterministic mock.",
            });

        Assert.Equal("Hello from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "HELLO")])).Text);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "hello there")]));
    }

    [Fact]
    public async Task AddResponses_SeedsJsonDeserializedResponsesInOrder()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            JsonSerializer.Deserialize<Dictionary<string, string>>(
                """
                {
                  "weather": "General weather response.",
                  "weather today": "Specific weather response."
                }
                """)!,
            static (request, key) => request.LastUserText?.Contains(key, StringComparison.OrdinalIgnoreCase) is true);

        Assert.Equal("Specific weather response.", (await client.GetResponseAsync([new(ChatRole.User, "weather today")])).Text);
    }

    [Fact]
    public async Task AddResponses_AppliesSingleUseToDictionaryResponses()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["hello:2"] = "Hello again. Nice to see you.",
                ["hello:1"] = "Hello. Nice to meet you.",
            },
            static (request, key) => string.Equals(
                request.LastUserText,
                key.Split(new[] { ':' }, 2)[0],
                StringComparison.OrdinalIgnoreCase),
            singleUse: true);

        Assert.Equal("Hello. Nice to meet you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("Hello again. Nice to see you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "hello")]));
    }

    [Fact]
    public async Task AddResponses_EnumerableResponsesSupportRepeatedKeys()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new KeyValuePair<string, string>[]
            {
                new("hello", "Hello again. Nice to see you."),
                new("hello", "Hello. Nice to meet you."),
            },
            singleUse: true);

        Assert.Equal("Hello. Nice to meet you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("Hello again. Nice to see you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "hello")]));
    }

    [Fact]
    public async Task AddResponses_AppliesResponseWrapperToStringResponses()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken observedToken = default;
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["hello"] = "Hello",
                ["goodbye"] = "Goodbye",
            },
            getResponse: (response, cancellationToken) =>
            {
                observedToken = cancellationToken;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"{response.Text} from wrapper")));
            });

        ChatResponse result = await client.GetResponseAsync(
            [new(ChatRole.User, "hello")],
            cancellationToken: cancellationTokenSource.Token);

        Assert.Equal("Hello from wrapper", result.Text);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
    }

    [Fact]
    public async Task AddResponse_ReturnsNonTextChatResponse()
    {
        using var client = new MockChatClient();
        client.AddResponse(
            static request => request.LastUserText == "get-weather",
            static (_, _) => Task.FromResult(
                new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        [new FunctionCallContent("weather-call", "GetWeather")]))));

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "get-weather")]);

        FunctionCallContent functionCall = Assert.IsType<FunctionCallContent>(
            Assert.Single(Assert.Single(response.Messages).Contents));
        Assert.Equal("weather-call", functionCall.CallId);
        Assert.Equal("GetWeather", functionCall.Name);
    }

    [Fact]
    public async Task AddResponses_MatchesImageRequestsByMediaType()
    {
        const string JpegResponse = "I see you shared a JPEG.";
        const string PngResponse = "I see you shared a PNG.";
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["image/jpeg"] = JpegResponse,
                ["image/png"] = PngResponse,
            },
            static (request, mediaType) => request.Messages
                .SelectMany(static message => message.Contents)
                .OfType<DataContent>()
                .Any(content => string.Equals(content.MediaType, mediaType, StringComparison.OrdinalIgnoreCase)));

        ChatResponse jpegResponse = await client.GetResponseAsync(
            [
                new ChatMessage(
                    ChatRole.User,
                    [new DataContent(new byte[] { 0xFF }, "image/jpeg")])
            ]);
        ChatResponse pngResponse = await client.GetResponseAsync(
            [
                new ChatMessage(
                    ChatRole.User,
                    [new DataContent(new byte[] { 0x89 }, "image/png")])
            ]);

        Assert.Equal(JpegResponse, jpegResponse.Text);
        Assert.Equal(PngResponse, pngResponse.Text);
    }

    [Fact]
    public async Task AddResponses_ResponseWrapperReceivesCancellationToken()
    {
        CancellationToken observedToken = default;
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["cancel"] = "unreachable",
                ["other"] = "Other response",
            },
            getResponse: async (response, cancellationToken) =>
            {
                observedToken = cancellationToken;
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return response;
            });
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "cancel")], cancellationToken: cancellationTokenSource.Token));

        Assert.Equal(cancellationTokenSource.Token, observedToken);
    }

    [Fact]
    public async Task ExtensibilityHooks_AreOverridable()
    {
        using var client = new ExtensibleMockChatClient();
        client.AddResponses(
            new()
            {
                ["hello"] = "Hello",
                ["goodbye"] = "Goodbye",
            });

        Assert.Equal("Hello", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal(1, client.AddResponsesFromDictionaryCallCount);
        Assert.Equal(1, client.AddResponsesFromEnumerableCallCount);
        Assert.Equal(2, client.AddSeedCallCount);
        Assert.Equal(1, client.MatchSeededResponseCallCount);
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

    [Fact]
    public async Task MockEmbeddingGenerator_InvokesConfiguredCallbackAndRecordsCall()
    {
        var values = new List<string> { "trail", "camp" };
        var options = new EmbeddingGenerationOptions();
        using var cancellationTokenSource = new CancellationTokenSource();
        var expected = new GeneratedEmbeddings<Embedding<float>>([new(new float[] { 1, 2, 3, 4 })]);

        using var generator = new MockEmbeddingGenerator<string>
        {
            GenerateAsyncCallback = (actualValues, actualOptions, cancellationToken) =>
            {
                Assert.Same(values, actualValues);
                Assert.Same(options, actualOptions);
                Assert.Equal(cancellationTokenSource.Token, cancellationToken);
                return Task.FromResult(expected);
            },
        };

        Assert.Same(expected, await generator.GenerateAsync(values, options, cancellationTokenSource.Token));
        Assert.Equal(1, generator.CallCount);
    }

    [Fact]
    public async Task MockEmbeddingGenerator_SupportsGenericInputs()
    {
        var value = new object();
        var expected = new GeneratedEmbeddings<Embedding<float>>([new(new float[] { 1, 2, 3, 4 })]);
        using var generator = new MockEmbeddingGenerator<object>
        {
            GenerateAsyncCallback = (values, _, _) =>
            {
                Assert.Same(value, Assert.Single(values));
                return Task.FromResult(expected);
            },
        };

        Assert.Same(expected, await generator.GenerateAsync([value]));
    }

    [Fact]
    public async Task MockEmbeddingGenerator_RecordsCallsForDerivedGenerators()
    {
        var expected = new GeneratedEmbeddings<Embedding<float>>([new(new float[] { 1, 2, 3, 4 })]);
        using var generator = new DerivedMockEmbeddingGenerator(expected);

        Assert.Same(expected, await generator.GenerateAsync(["trail"]));
        Assert.Equal(1, generator.CallCount);
    }

    [Fact]
    public void MockEmbeddingGenerator_ConfiguresServiceResolution()
    {
        using var generator = new MockEmbeddingGenerator<string>();
        Assert.Same(generator, generator.GetService(typeof(MockEmbeddingGenerator<string>)));

        var expected = new object();
        generator.GetServiceCallback = static (_, _) => null;
        Assert.Null(generator.GetService(typeof(object)));

        generator.GetServiceCallback = (_, _) => expected;
        Assert.Same(expected, generator.GetService(typeof(object)));
    }

    private sealed class ExtensibleMockChatClient : MockChatClient
    {
        public int AddSeedCallCount { get; private set; }

        public int AddResponsesFromDictionaryCallCount { get; private set; }

        public int AddResponsesFromEnumerableCallCount { get; private set; }

        public int MatchSeededResponseCallCount { get; private set; }

        protected override MockChatClient AddSeed(
            Func<MockChatClientRequest, bool> requestPredicate,
            Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> getResponse,
            Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> getStreamingResponse,
            bool singleUse)
        {
            AddSeedCallCount++;
            return base.AddSeed(requestPredicate, getResponse, getStreamingResponse, singleUse);
        }

        protected override MockChatClient AddResponsesFromDictionary(
            Dictionary<string, string> responses,
            Func<MockChatClientRequest, string, bool>? requestPredicate,
            bool singleUse,
            Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse)
        {
            AddResponsesFromDictionaryCallCount++;
            return base.AddResponsesFromDictionary(responses, requestPredicate, singleUse, getResponse);
        }

        protected override MockChatClient AddResponsesFromEnumerable(
            IEnumerable<KeyValuePair<string, string>> responses,
            Func<MockChatClientRequest, string, bool>? requestPredicate,
            bool singleUse,
            Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse)
        {
            AddResponsesFromEnumerableCallCount++;
            return base.AddResponsesFromEnumerable(responses, requestPredicate, singleUse, getResponse);
        }

        protected override SeededResponse MatchSeededResponse(MockChatClientRequest request)
        {
            MatchSeededResponseCallCount++;
            return base.MatchSeededResponse(request);
        }
    }

    private sealed class DerivedMockEmbeddingGenerator(GeneratedEmbeddings<Embedding<float>> expected) : MockEmbeddingGenerator<string>
    {
        protected override Task<GeneratedEmbeddings<Embedding<float>>> GenerateCoreAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options,
            CancellationToken cancellationToken) =>
            Task.FromResult(expected);
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

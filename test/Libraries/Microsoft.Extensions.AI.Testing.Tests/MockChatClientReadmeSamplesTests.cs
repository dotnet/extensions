// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class MockChatClientReadmeSamplesTests
{
    [Fact]
    public async Task QuickStart()
    {
        using var client = new MockChatClient();
        client
            .AddResponse(
                static _ => true,
                static (_, _) => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fallback response."))),
                singleUse: true)
            .AddResponse(
                static request => request.LastUserText?.Contains("hello", StringComparison.OrdinalIgnoreCase) is true,
                static (_, _) => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from a deterministic mock."))));

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "hello")]);
        Console.WriteLine(response.Text);

        Assert.Equal("Hello from a deterministic mock.", response.Text);
    }

    [Fact]
    public async Task ResponseDictionary()
    {
        using var client = new MockChatClient();
        client.AddResponses(new()
        {
            ["hello"] = "Hello from a deterministic mock.",
            ["goodbye"] = "Goodbye from a deterministic mock.",
        });

        Assert.Equal("Hello from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("Goodbye from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "goodbye")])).Text);
    }

    [Fact]
    public async Task JsonResponseDictionary()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            JsonSerializer.Deserialize<Dictionary<string, string>>(
            """
            {
              "hello": "Hello from a deterministic mock.",
              "goodbye": "Goodbye from a deterministic mock."
            }
            """)!);

        Assert.Equal("Hello from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("Goodbye from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "goodbye")])).Text);
    }

    [Fact]
    public async Task EnumerableResponseDictionary()
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
    }

    [Fact]
    public async Task FunctionCallResponse()
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
    public async Task CustomDictionaryPredicate()
    {
        using var client = new MockChatClient();

        client.AddResponses(
            new()
            {
                ["hello"] = "Hello from a deterministic mock.",
                ["goodbye"] = "Goodbye from a deterministic mock.",
            },
            static (request, key) => request.LastUserText?.StartsWith(key, StringComparison.OrdinalIgnoreCase) is true);

        Assert.Equal("Hello from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "hello there")])).Text);
        Assert.Equal("Goodbye from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "goodbye for now")])).Text);
    }

    [Fact]
    public async Task SingleUseDictionaryResponses()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["hello:2"] = "Hello again. Nice to see you.",
                ["hello:1"] = "Hello. Nice to meet you.",
            },
            static (request, key) =>
            {
                string promptKey = key.Split(new[] { ':' }, 2)[0];
                return string.Equals(request.LastUserText, promptKey, StringComparison.OrdinalIgnoreCase);
            },
            singleUse: true);

        Assert.Equal("Hello. Nice to meet you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
        Assert.Equal("Hello again. Nice to see you.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
    }

    [Fact]
    public async Task ImageResponseDictionary()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["image/jpeg"] = "I see you shared a JPEG.",
                ["image/png"] = "I see you shared a PNG.",
            },
            static (request, mediaType) => request.Messages
                .SelectMany(static message => message.Contents)
                .OfType<DataContent>()
                .Any(content => string.Equals(content.MediaType, mediaType, StringComparison.OrdinalIgnoreCase)));

        Assert.Equal(
            "I see you shared a JPEG.",
            (await client.GetResponseAsync([new(ChatRole.User, [new DataContent(new byte[] { 0xFF }, "image/jpeg")])])).Text);
        Assert.Equal(
            "I see you shared a PNG.",
            (await client.GetResponseAsync([new(ChatRole.User, [new DataContent(new byte[] { 0x89 }, "image/png")])])).Text);
    }

    [Fact]
    public async Task ResponseDictionaryWrapper()
    {
        using var client = new MockChatClient();
        client.AddResponses(
            new()
            {
                ["hello"] = "Hello from a deterministic mock.",
                ["goodbye"] = "Goodbye from a deterministic mock.",
            },
            getResponse: async (response, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                return response;
            });

        Assert.Equal("Hello from a deterministic mock.", (await client.GetResponseAsync([new(ChatRole.User, "hello")])).Text);
    }

    [Fact]
    public async Task NonStreamingResponse()
    {
        using var client = new MockChatClient();

        client.AddResponse(
            static request => request.LastUserText == "explain",
            static (_, _) => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, "Complete answer"))));

        Assert.Equal("Complete answer", (await client.GetResponseAsync([new(ChatRole.User, "explain")])).Text);
    }

    [Fact]
    public async Task StreamingResponse()
    {
        using var client = new MockChatClient();

        client.AddStreamingResponse(
            static request => request.LastUserText == "stream",
            static (_, cancellationToken) => GetUpdatesAsync(cancellationToken));

        static async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 1 ");
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 2");
        }

        List<ChatResponseUpdate> updates = await ToListAsync(client.GetStreamingResponseAsync([new(ChatRole.User, "stream")]));
        Assert.Equal("Part 1 ", updates[0].Text);
        Assert.Equal("Part 2", updates[1].Text);
    }

    [Fact]
    public async Task DistinctStreamingAndNonStreamingResponses()
    {
        using var client = new MockChatClient();

        client.AddResponse(
            static request => request.LastUserText == "both",
            static (_, _) => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, "Complete answer"))),
            static (_, cancellationToken) => GetUpdatesAsync(cancellationToken));

        static async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 1 ");
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 2");
        }

        Assert.Equal("Complete answer", (await client.GetResponseAsync([new(ChatRole.User, "both")])).Text);
        List<ChatResponseUpdate> updates = await ToListAsync(client.GetStreamingResponseAsync([new(ChatRole.User, "both")]));
        Assert.Equal("Part 1 ", updates[0].Text);
        Assert.Equal("Part 2", updates[1].Text);
    }

    [Fact]
    public async Task RichChatResponse()
    {
        using var client = new MockChatClient();

        client.AddResponse(
            static request => request.LastUserText == "sources",
            static (_, _) => Task.FromResult(
                new ChatResponse(
                [
                    new ChatMessage(
                        ChatRole.Assistant,
                        [
                            new TextContent("TrailMaster tracks your progress.")
                            {
                                Annotations =
                                [
                                    new CitationAnnotation
                                    {
                                        Title = "Example_GPS_Watch.md",
                                        Snippet = "track your progress",
                                    }
                                ]
                            },
                            new TextReasoningContent("The document describes tracking features.")
                        ])
                ])
                {
                    Usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 7 }
                }));

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "sources")]);
        ChatMessage message = Assert.Single(response.Messages);
        TextContent content = Assert.IsType<TextContent>(message.Contents[0]);

        Assert.Equal("TrailMaster tracks your progress.", content.Text);
        Assert.Equal("Example_GPS_Watch.md", Assert.IsType<CitationAnnotation>(Assert.Single(content.Annotations!)).Title);
        Assert.Equal("The document describes tracking features.", Assert.IsType<TextReasoningContent>(message.Contents[1]).Text);
        Assert.Equal(3, response.Usage!.InputTokenCount);
        Assert.Equal(7, response.Usage.OutputTokenCount);
    }

    [Fact]
    public async Task RequestAssertions()
    {
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (_, _) => Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"))));
        _ = await client.GetResponseAsync([new(ChatRole.User, "hello")]);

        MockChatClientRequest first = client.Requests[0];
        string? lastUserText = first.LastUserText;
        bool wasStreaming = first.IsStreaming;

        Assert.Equal("hello", lastUserText);
        Assert.False(wasStreaming);
    }

    [Fact]
    public async Task ClearAndReseed()
    {
        using var client = new MockChatClient();
        client.AddResponse(static _ => true, static (_, _) => Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "old scenario"))));
        _ = await client.GetResponseAsync([new(ChatRole.User, "old")]);

        client.ClearResponses()
            .AddResponse(
                static _ => true,
                static (_, _) => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "New test scenario."))));

        Assert.Equal("New test scenario.", (await client.GetResponseAsync([new(ChatRole.User, "new")])).Text);
        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task MockErrors()
    {
        using var client = new MockChatClient();

        client.AddException(
            static request => request.LastUserText == "UNAVAILABLE",
            static () => new HttpRequestException("The provider is temporarily unavailable."));

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "UNAVAILABLE")]));
        Assert.Equal("The provider is temporarily unavailable.", exception.Message);
    }

    [Fact]
    public async Task MockEmbeddings()
    {
        using var embeddings = new MockEmbeddingGenerator<string>
        {
            GenerateAsyncCallback = static (_, _, _) =>
                Task.FromResult<GeneratedEmbeddings<Embedding<float>>>(
                    [new(new float[] { 0.1f, 0.2f, 0.3f })]),
        };

        GeneratedEmbeddings<Embedding<float>> result = await embeddings.GenerateAsync(["trail"]);
        Console.WriteLine(embeddings.CallCount);

        Assert.Equal(1, embeddings.CallCount);
        Assert.Equal(new float[] { 0.1f, 0.2f, 0.3f }, Assert.Single(result).Vector.ToArray());
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

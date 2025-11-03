// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class SentimentEnricherTests
{
    private static readonly IngestionDocument _document = new("test");

    [Fact]
    public void ThrowsOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>("options", () => new SentimentEnricher(null!));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ThrowsOnInvalidThreshold(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>("confidenceThreshold", () => new SentimentEnricher(new(new TestChatClient()), confidenceThreshold: threshold));
    }

    [Fact]
    public async Task ThrowsOnNullChunks()
    {
        using TestChatClient chatClient = new();
        SentimentEnricher sut = new(new(chatClient));

        await Assert.ThrowsAsync<ArgumentNullException>("chunks", async () =>
        {
            await foreach (var _ in sut.ProcessAsync(null!))
            {
                // No-op
            }
        });
    }

    [Fact]
    public async Task CanProvideSentiment()
    {
        int counter = 0;
        string[] sentiments = { "Positive", "Negative", "Neutral", "Unknown" };
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var materializedMessages = messages.ToArray();

                Assert.Equal(2, materializedMessages.Length);
                Assert.Equal(ChatRole.System, materializedMessages[0].Role);
                Assert.Equal(ChatRole.User, materializedMessages[1].Role);

                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, sentiments[counter++])
                }));
            }
        };
        SentimentEnricher sut = new(new(chatClient));
        var input = CreateChunks().ToAsyncEnumerable();

        var chunks = await sut.ProcessAsync(input).ToListAsync();

        Assert.Equal(4, chunks.Count);

        Assert.Equal("Positive", chunks[0].Metadata[SentimentEnricher.MetadataKey]);
        Assert.Equal("Negative", chunks[1].Metadata[SentimentEnricher.MetadataKey]);
        Assert.Equal("Neutral", chunks[2].Metadata[SentimentEnricher.MetadataKey]);
        Assert.Equal("Unknown", chunks[3].Metadata[SentimentEnricher.MetadataKey]);
    }

    [Fact]
    public async Task ThrowsOnInvalidResponse()
    {
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, "Unexpected result!")
                }));
            }
        };

        SentimentEnricher sut = new(new(chatClient));
        var input = CreateChunks().ToAsyncEnumerable();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in sut.ProcessAsync(input))
            {
                // No-op
            }
        });
    }

    private static List<IngestionChunk<string>> CreateChunks() =>
    [
        new("I love programming! It's so much fun and rewarding.", _document),
        new("I hate bugs. They are so frustrating and time-consuming.", _document),
        new("The weather is okay, not too bad but not great either.", _document),
        new("I hate you. I am sorry, I actually don't. I am not sure myself what my feelings are.", _document)
    ];
}

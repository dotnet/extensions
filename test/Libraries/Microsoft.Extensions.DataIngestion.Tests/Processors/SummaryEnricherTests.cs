// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class SummaryEnricherTests
{
    private static readonly IngestionDocument _document = new("test");

    [Fact]
    public void ThrowsOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>("options", () => new SummaryEnricher(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ThrowsOnInvalidMaxKeywords(int wordCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>("maxWordCount", () => new SummaryEnricher(new(new TestChatClient()), maxWordCount: wordCount));
    }

    [Fact]
    public async Task ThrowsOnNullChunks()
    {
        using TestChatClient chatClient = new();
        SummaryEnricher sut = new(new(chatClient));

        await Assert.ThrowsAsync<ArgumentNullException>("chunks", async () =>
        {
            await foreach (var _ in sut.ProcessAsync(null!))
            {
                // No-op
            }
        });
    }

    [Fact]
    public async Task CanProvideSummary()
    {
        int counter = 0;
        string[] summaries = { "First summary.", "Second summary." };
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Equal(0, counter++);
                var materializedMessages = messages.ToArray();

                Assert.Equal(2, materializedMessages.Length);
                Assert.Equal(ChatRole.System, materializedMessages[0].Role);
                Assert.Equal(ChatRole.User, materializedMessages[1].Role);

                string response = JsonSerializer.Serialize(new Envelope<string[]> { data = summaries });
                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, response)
                }));
            }
        };
        SummaryEnricher sut = new(new(chatClient));
        var input = CreateChunks().ToAsyncEnumerable();

        var chunks = await sut.ProcessAsync(input).ToListAsync();

        Assert.Equal(2, chunks.Count);
        Assert.Equal(summaries[0], (string)chunks[0].Metadata[SummaryEnricher.MetadataKey]!);
        Assert.Equal(summaries[1], (string)chunks[1].Metadata[SummaryEnricher.MetadataKey]!);
    }

    private static List<IngestionChunk<string>> CreateChunks() =>
    [
        new("I love programming! It's so much fun and rewarding.", _document),
        new("I hate bugs. They are so frustrating and time-consuming.", _document)
    ];
}

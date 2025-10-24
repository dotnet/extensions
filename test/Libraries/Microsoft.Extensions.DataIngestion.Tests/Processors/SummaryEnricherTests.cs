// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class SummaryEnricherTests
{
    private static readonly IngestionDocument _document = new("test");

    [Fact]
    public void ThrowsOnNullChatClient()
        => Assert.Throws<ArgumentNullException>(() => new SummaryEnricher(null!));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ThrowsOnInvalidMaxKeywords(int wordCount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new SummaryEnricher(new TestChatClient(), maxWordCount: wordCount));
        Assert.Equal("maxWordCount", ex.ParamName);
    }

    [Fact]
    public async Task ThrowsOnNullChunks()
    {
        using TestChatClient chatClient = new();
        SummaryEnricher sut = new(chatClient);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
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
                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, summaries[counter++])
                }));
            }
        };
        SummaryEnricher sut = new(chatClient);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class KeywordEnricherTests
{
    private static readonly IngestionDocument _document = new("test");

    [Fact]
    public void ThrowsOnNullChatClient()
        => Assert.Throws<ArgumentNullException>(() => new KeywordEnricher(null!, predefinedKeywords: null, confidenceThreshold: 0.5));

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ThrowsOnInvalidThreshold(double threshold)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KeywordEnricher(new TestChatClient(), predefinedKeywords: null, confidenceThreshold: threshold));
        Assert.Equal("confidenceThreshold", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ThrowsOnInvalidMaxKeywords(int keywordCount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KeywordEnricher(new TestChatClient(), predefinedKeywords: null, maxKeywords: keywordCount));
        Assert.Equal("maxKeywords", ex.ParamName);
    }

    [Fact]
    public async Task ThrowsOnNullChunks()
    {
        using TestChatClient chatClient = new();
        KeywordEnricher sut = new(chatClient, predefinedKeywords: null, confidenceThreshold: 0.5);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in sut.ProcessAsync(null!))
            {
                // No-op
            }
        });
    }

    [Theory]
    [InlineData]
    [InlineData("AI", "MEAI", "Animals", "Rabbits")]
    public async Task CanExtractKeywords(params string[] predefined)
    {
        int counter = 0;
        string[] keywords = { "AI;MEAI", "Animals;Rabbits" };
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, keywords[counter++])
                }));
            }
        };

        KeywordEnricher sut = new(chatClient, predefinedKeywords: predefined, confidenceThreshold: 0.5);
        var chunks = CreateChunks().ToAsyncEnumerable();

        IReadOnlyList<IngestionChunk<string>> got = await sut.ProcessAsync(chunks).ToListAsync();

        Assert.Equal(["AI", "MEAI"], (string[])got[0].Metadata[KeywordEnricher.MetadataKey]);
        Assert.Equal(["Animals", "Rabbits"], (string[])got[1].Metadata[KeywordEnricher.MetadataKey]);
    }

    private static List<IngestionChunk<string>> CreateChunks() =>
    [
        new("The Microsoft.Extensions.AI libraries provide a unified approach for representing generative AI components", _document),
        new("Rabbits are great pets. They are friendly and make excellent companions.", _document)
    ];
}

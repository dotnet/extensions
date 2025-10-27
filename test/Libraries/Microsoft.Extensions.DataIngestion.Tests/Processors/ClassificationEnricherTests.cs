// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class ClassificationEnricherTests
{
    private static readonly IngestionDocument _document = new("test");

    [Fact]
    public void ThrowsOnNullChatClient()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ClassificationEnricher(null!, predefinedClasses: ["some"]));
        Assert.Equal("chatClient", ex.ParamName);
    }

    [Fact]
    public void ThrowsOnEmptyPredefinedClasses()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ClassificationEnricher(new TestChatClient(), predefinedClasses: []));
        Assert.Equal("predefinedClasses", ex.ParamName);
    }

    [Fact]
    public void ThrowsOnDuplicatePredefinedClasses()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ClassificationEnricher(new TestChatClient(), predefinedClasses: ["same", "same"]));
        Assert.Equal("predefinedClasses", ex.ParamName);
    }

    [Fact]
    public void ThrowsOnPredefinedClassesContainingFallback()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ClassificationEnricher(new TestChatClient(), predefinedClasses: ["same", "Unknown"]));
        Assert.Equal("predefinedClasses", ex.ParamName);
    }

    [Fact]
    public void ThrowsOnFallbackInPredefinedClasses()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ClassificationEnricher(new TestChatClient(), predefinedClasses: ["some"], fallbackClass: "some"));
        Assert.Equal("predefinedClasses", ex.ParamName);
    }

    [Fact]
    public void ThrowsOnPredefinedClassesContainingComma()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ClassificationEnricher(new TestChatClient(), predefinedClasses: ["n,t"]));
        Assert.Equal("predefinedClasses", ex.ParamName);
    }

    [Fact]
    public async Task ThrowsOnNullChunks()
    {
        using TestChatClient chatClient = new();
        ClassificationEnricher sut = new(chatClient, predefinedClasses: ["some"]);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in sut.ProcessAsync(null!))
            {
                // No-op
            }
        });
    }

    [Fact]
    public async Task CanClassify()
    {
        int counter = 0;
        string[] classes = ["AI", "Animals", "UFO"];
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, classes[counter++])
                }));
            }
        };
        ClassificationEnricher sut = new(chatClient, ["AI", "Animals", "Sports"], fallbackClass: "UFO");

        IReadOnlyList<IngestionChunk<string>> got = await sut.ProcessAsync(CreateChunks().ToAsyncEnumerable()).ToListAsync();

        Assert.Equal(3, got.Count);
        Assert.Equal("AI", got[0].Metadata[ClassificationEnricher.MetadataKey]);
        Assert.Equal("Animals", got[1].Metadata[ClassificationEnricher.MetadataKey]);
        Assert.Equal("UFO", got[2].Metadata[ClassificationEnricher.MetadataKey]);
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

        ClassificationEnricher sut = new(chatClient, ["AI", "Animals", "Sports"]);
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
        new(".NET developers need to integrate and interact with a growing variety of artificial intelligence (AI) services in their apps. " +
            "The Microsoft.Extensions.AI libraries provide a unified approach for representing generative AI components, and enable seamless" +
            " integration and interoperability with various AI services.", _document),
        new ("Rabbits are small mammals in the family Leporidae of the order Lagomorpha (along with the hare and the pika)." +
            "They are herbivorous animals and are known for their long ears, large hind legs, and short fluffy tails.", _document),
        new("This text does not belong to any category.", _document),
    ];
}

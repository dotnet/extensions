// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class AlternativeTextEnricherTests
{
    [Fact]
    public void ThrowsOnNullChatClient()
        => Assert.Throws<ArgumentNullException>(() => new ImageAlternativeTextEnricher(null!));

    [Fact]
    public async Task ThrowsOnNullDocument()
    {
        using TestChatClient chatClient = new();

        ImageAlternativeTextEnricher sut = new(chatClient);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ProcessAsync(null!));
    }

    [Fact]
    public async Task CanGenerateImageAltText()
    {
        const string PreExistingAltText = "Pre-existing alt text";
        ReadOnlyMemory<byte> imageContent = new byte[256];

        int counter = 0;
        string[] descriptions = { "First alt text", "Second alt text" };
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var message = Assert.Single(messages);
                DataContent dataContent = Assert.IsType<DataContent>(message.Contents[1]);
                Assert.Equal("image/png", dataContent.MediaType);
                Assert.Equal(imageContent.ToArray(), dataContent.Data.ToArray());

                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, descriptions[counter++])
                }));
            }
        };
        ImageAlternativeTextEnricher sut = new(chatClient);

        IngestionDocumentImage documentImage = new($"![](nonExisting.png)")
        {
            AlternativeText = null,
            Content = imageContent,
            MediaType = "image/png"
        };

        IngestionDocumentImage tableCell = new($"![](another.png)")
        {
            AlternativeText = null,
            Content = imageContent,
            MediaType = "image/png"
        };

        IngestionDocumentImage imageWithAltText = new($"![](noChangesNeeded.png)")
        {
            AlternativeText = PreExistingAltText,
            Content = imageContent,
            MediaType = "image/png"
        };

        IngestionDocumentImage imageWithNoContent = new($"![](noImage.png)")
        {
            AlternativeText = null,
            Content = default,
            MediaType = "image/png"
        };

        IngestionDocument document = new("withImage")
        {
            Sections =
            {
                new IngestionDocumentSection
                {
                    Elements =
                    {
                        documentImage,
                        new IngestionDocumentTable("nvm", new[,] { { tableCell } })
                    }
                }
            }
        };

        await sut.ProcessAsync(document);

        Assert.Equal(descriptions[0], documentImage.AlternativeText);
        Assert.Equal(descriptions[1], tableCell.AlternativeText);
        Assert.Same(PreExistingAltText, imageWithAltText.AlternativeText);
        Assert.Null(imageWithNoContent.AlternativeText);
    }
}

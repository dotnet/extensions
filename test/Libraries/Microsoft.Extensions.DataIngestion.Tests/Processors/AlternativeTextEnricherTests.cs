// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Processors.Tests;

public class AlternativeTextEnricherTests
{
    [Fact]
    public void ThrowsOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>("options", () => new ImageAlternativeTextEnricher(null!));
    }

    [Fact]
    public async Task ThrowsOnNullDocument()
    {
        using TestChatClient chatClient = new();

        ImageAlternativeTextEnricher sut = new(new(chatClient));

        await Assert.ThrowsAsync<ArgumentNullException>("document", async () => await sut.ProcessAsync(null!));
    }

    [Fact]
    public async Task CanGenerateImageAltText()
    {
        const string PreExistingAltText = "Pre-existing alt text";
        ReadOnlyMemory<byte> imageContent = new byte[256];

        string[] descriptions = { "First alt text", "Second alt text" };
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var materializedMessages = messages.ToArray();

                Assert.Equal(2, materializedMessages.Length);
                Assert.Equal(ChatRole.System, materializedMessages[0].Role);
                Assert.Equal(ChatRole.User, materializedMessages[1].Role);
                Assert.Equal(2, materializedMessages[1].Contents.Count);

                Assert.All(materializedMessages[1].Contents, content =>
                {
                    DataContent dataContent = Assert.IsType<DataContent>(content);
                    Assert.Equal("image/png", dataContent.MediaType);
                    Assert.Equal(imageContent.ToArray(), dataContent.Data.ToArray());
                });

                return Task.FromResult(new ChatResponse(new[]
                {
                    new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(new Envelope<string[]> { data = descriptions }))
                }));
            }
        };
        ImageAlternativeTextEnricher sut = new(new(chatClient));

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

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 7)]
    [InlineData(15, 3)]
    public async Task SendsOneRequestPerBatchSize(int batchSize, int batchCount)
    {
        ReadOnlyMemory<byte> imageContent = new byte[256];

        int callsCount = 0;
        using TestChatClient chatClient = new()
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                callsCount++;

                var materializedMessages = messages.ToArray();

                // One system message + one User message with all the contents
                Assert.Equal(2, materializedMessages.Length);
                Assert.Equal(ChatRole.System, materializedMessages[0].Role);
                Assert.Equal(ChatRole.User, materializedMessages[1].Role);
                Assert.Equal(batchSize, materializedMessages[1].Contents.Count);

                Assert.All(materializedMessages[1].Contents, content =>
                {
                    DataContent dataContent = Assert.IsType<DataContent>(content);
                    Assert.Equal("image/png", dataContent.MediaType);
                    Assert.Equal(imageContent.ToArray(), dataContent.Data.ToArray());
                });

                Envelope<string[]> data = new() { data = Enumerable.Range(0, batchSize).Select(i => i.ToString()).ToArray() };
                return Task.FromResult(new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(data)) }));
            }
        };

        ImageAlternativeTextEnricher sut = new(new(chatClient) { BatchSize = batchSize });

        IngestionDocumentSection rootSection = new();
        for (int i = 0; i < batchSize * batchCount; i++)
        {
            IngestionDocumentImage image = new($"![](image{i}.png)")
            {
                Content = imageContent,
                MediaType = "image/png",
                AlternativeText = null
            };

            rootSection.Elements.Add(image);
        }

        IngestionDocument document = new("batchTest")
        {
            Sections = { rootSection }
        };

        await sut.ProcessAsync(document);
        Assert.Equal(batchCount, callsCount);
    }
}

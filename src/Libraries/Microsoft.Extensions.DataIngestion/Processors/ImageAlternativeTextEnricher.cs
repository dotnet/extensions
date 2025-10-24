// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Enriches <see cref="IngestionDocumentImage"/> elements with alternative text using an AI service,
/// so the generated embeddings can include the image content information.
/// </summary>
public sealed class ImageAlternativeTextEnricher : IngestionDocumentProcessor
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;

    public ImageAlternativeTextEnricher(IChatClient chatClient, ChatOptions? chatOptions = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatOptions = chatOptions;
    }

    public override async Task<IngestionDocument> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        foreach (var element in document.EnumerateContent())
        {
            if (element is IngestionDocumentImage image)
            {
                await ProcessAsync(image, cancellationToken);
            }
            else if (element is IngestionDocumentTable table)
            {
                foreach (var cell in table.Cells)
                {
                    if (cell is IngestionDocumentImage cellImage)
                    {
                        await ProcessAsync(cellImage, cancellationToken);
                    }
                }
            }
        }

        return document;
    }

    private async Task ProcessAsync(IngestionDocumentImage image, CancellationToken cancellationToken)
    {
        if (image.Content.HasValue && !string.IsNullOrEmpty(image.MediaType)
            && string.IsNullOrEmpty(image.AlternativeText))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User,
                [
                    new TextContent("Write a detailed alternative text for this image with less than 50 words."),
                    new DataContent(image.Content.Value, image.MediaType!),
                ])
            ], _chatOptions, cancellationToken: cancellationToken);

            image.AlternativeText = response.Text;
        }
    }
}

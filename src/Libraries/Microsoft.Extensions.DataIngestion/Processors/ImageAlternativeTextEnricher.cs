// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Enriches <see cref="IngestionDocumentImage"/> elements with alternative text using an AI service,
/// so the generated embeddings can include the image content information.
/// </summary>
public sealed class ImageAlternativeTextEnricher : IngestionDocumentProcessor
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly TextContent _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageAlternativeTextEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used to get responses for generating alternative text.</param>
    /// <param name="chatOptions">Options for the chat client.</param>
    public ImageAlternativeTextEnricher(IChatClient chatClient, ChatOptions? chatOptions = null)
    {
        _chatClient = Throw.IfNull(chatClient);
        _chatOptions = chatOptions;
        _request = new("Write a detailed alternative text for this image with less than 50 words.");
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(document);

        foreach (var element in document.EnumerateContent())
        {
            if (element is IngestionDocumentImage image)
            {
                await ProcessAsync(image, cancellationToken).ConfigureAwait(false);
            }
            else if (element is IngestionDocumentTable table)
            {
                foreach (var cell in table.Cells)
                {
                    if (cell is IngestionDocumentImage cellImage)
                    {
                        await ProcessAsync(cellImage, cancellationToken).ConfigureAwait(false);
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
                    _request,
                    new DataContent(image.Content.Value, image.MediaType!),
                ])
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            image.AlternativeText = response.Text;
        }
    }
}

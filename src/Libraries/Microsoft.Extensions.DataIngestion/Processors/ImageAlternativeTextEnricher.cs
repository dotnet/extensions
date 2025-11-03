// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    private readonly EnricherOptions _options;
    private readonly ChatMessage _systemPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageAlternativeTextEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for generating alternative text.</param>
    public ImageAlternativeTextEnricher(EnricherOptions options)
    {
        _options = Throw.IfNull(options).Clone();
        _systemPrompt = new(ChatRole.System, "For each of the following images, write a detailed alternative text with fewer than 50 words.");
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(document);

        List<IngestionDocumentImage>? batch = null;

        foreach (var element in document.EnumerateContent())
        {
            if (element is IngestionDocumentImage image)
            {
                if (ShouldProcess(image))
                {
                    batch ??= new(_options.BatchSize);
                    batch.Add(image);

                    if (batch.Count == _options.BatchSize)
                    {
                        await ProcessAsync(batch, cancellationToken).ConfigureAwait(false);
                        batch.Clear();
                    }
                }
            }
            else if (element is IngestionDocumentTable table)
            {
                foreach (var cell in table.Cells)
                {
                    if (cell is IngestionDocumentImage cellImage && ShouldProcess(cellImage))
                    {
                        batch ??= new(_options.BatchSize);
                        batch.Add(cellImage);

                        if (batch.Count == _options.BatchSize)
                        {
                            await ProcessAsync(batch, cancellationToken).ConfigureAwait(false);
                            batch.Clear();
                        }
                    }
                }
            }
        }

        if (batch?.Count > 0)
        {
            await ProcessAsync(batch, cancellationToken).ConfigureAwait(false);
        }

        return document;
    }

    private static bool ShouldProcess(IngestionDocumentImage img) =>
        img.Content.HasValue && !string.IsNullOrEmpty(img.MediaType) && string.IsNullOrEmpty(img.AlternativeText);

    private async Task ProcessAsync(List<IngestionDocumentImage> batch, CancellationToken cancellationToken)
    {
        List<AIContent> contents = new(batch.Count);
        foreach (var image in batch)
        {
            contents.Add(new DataContent(image.Content!.Value, image.MediaType!));
        }

        var response = await _options.ChatClient.GetResponseAsync<string[]>(
            [_systemPrompt, new(ChatRole.User, contents)],
            _options.ChatOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response.Result.Length != contents.Count)
        {
            throw new InvalidOperationException($"The AI chat service returned {response.Result.Length} instead of {contents.Count} results.");
        }

        for (int i = 0; i < response.Result.Length; i++)
        {
            batch[i].AlternativeText = response.Result[i];
        }
    }
}

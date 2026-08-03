// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEDE0001 // Document extraction abstractions are experimental.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Reads documents by extracting structured OCR output using an <see cref="IDocumentExtractionClient"/>.
/// </summary>
public sealed class OcrDocumentReader : IngestionDocumentReader
{
    private const string BoundingBoxMetadataKey = "bounding_box";
    private const string BoundingRegionMetadataKey = "bounding_region";

    private readonly IDocumentExtractionClient _documentExtractionClient;
    private readonly DocumentExtractionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrDocumentReader"/> class.
    /// </summary>
    /// <param name="documentExtractionClient">The OCR client to use for document extraction.</param>
    /// <param name="options">Optional OCR options.</param>
    public OcrDocumentReader(IDocumentExtractionClient documentExtractionClient, DocumentExtractionOptions? options = null)
    {
        _documentExtractionClient = Throw.IfNull(documentExtractionClient);
        _options = options?.Clone() ?? new DocumentExtractionOptions();
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfNullOrEmpty(identifier);
        _ = Throw.IfNullOrEmpty(mediaType);

        DocumentExtractionResult documentExtractionResult = await _documentExtractionClient
            .ExtractAsync(source, mediaType, _options.Clone(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Map(documentExtractionResult, identifier);
    }

    private static IngestionDocument Map(DocumentExtractionResult documentExtractionResult, string identifier)
    {
        IngestionDocument document = new(identifier);

        foreach (DocumentPage page in documentExtractionResult.Pages)
        {
            IngestionDocumentSection section = new();
            int pageNumber = page.PageNumber;

            if (!string.IsNullOrWhiteSpace(page.Text))
            {
                section.Elements.Add(new IngestionDocumentParagraph(page.Text)
                {
                    Text = page.Text,
                    PageNumber = pageNumber
                });
            }

            foreach (DocumentImage image in page.Elements.OfType<DocumentImage>())
            {
                section.Elements.Add(MapImage(image, pageNumber));
            }

            document.Sections.Add(section);
        }

        return document;
    }

    private static IngestionDocumentImage MapImage(DocumentImage image, int pageNumber)
    {
        DataContent? content = image.Content;
        IngestionDocumentImage element = new(CreateImageMarkdown(image))
        {
            Content = content?.Data,
            MediaType = content?.MediaType,
            AlternativeText = image.Caption,
            PageNumber = image.BoundingRegion?.PageNumber ?? pageNumber
        };

        if (image.BoundingRegion is not null)
        {
            if (image.BoundingRegion.GetBounds() is { } bounds)
            {
                (float left, float top, float right, float bottom) = bounds;
                element.Metadata[BoundingBoxMetadataKey] = new[] { left, top, right, bottom };
            }

            element.Metadata[BoundingRegionMetadataKey] = image.BoundingRegion.Polygon.SelectMany(static p => new[] { p.X, p.Y }).ToArray();
        }

        return element;
    }

    private static string CreateImageMarkdown(DocumentImage image)
    {
        string altText = image.Caption ?? string.Empty;
        string uri = image.Content?.Uri ?? string.Empty;

        return $"![{altText}]({uri})";
    }
}

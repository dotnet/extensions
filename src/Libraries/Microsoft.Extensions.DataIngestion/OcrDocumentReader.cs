// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001 // OCR abstractions are experimental.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Reads documents by extracting structured OCR output using an <see cref="IOcrClient"/>.
/// </summary>
public sealed class OcrDocumentReader : IngestionDocumentReader
{
    private const string BoundingBoxMetadataKey = "bounding_box";
    private const string BoundingRegionMetadataKey = "bounding_region";

    private readonly IOcrClient _ocrClient;
    private readonly OcrOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrDocumentReader"/> class.
    /// </summary>
    /// <param name="ocrClient">The OCR client to use for document extraction.</param>
    /// <param name="options">Optional OCR options. When not provided, image extraction is requested.</param>
    public OcrDocumentReader(IOcrClient ocrClient, OcrOptions? options = null)
    {
        _ocrClient = Throw.IfNull(ocrClient);
        _options = options?.Clone() ?? new OcrOptions { IncludeImages = true };
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfNullOrEmpty(identifier);
        _ = Throw.IfNullOrEmpty(mediaType);

        OcrResult ocrResult = await _ocrClient
            .ExtractAsync(source, mediaType, _options.Clone(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Map(ocrResult, identifier);
    }

    private static IngestionDocument Map(OcrResult ocrResult, string identifier)
    {
        IngestionDocument document = new(identifier);

        foreach (OcrPage page in ocrResult.Pages)
        {
            IngestionDocumentSection section = new();
            int pageNumber = page.Index + 1;

            if (!string.IsNullOrWhiteSpace(page.Markdown))
            {
                section.Elements.Add(new IngestionDocumentParagraph(page.Markdown)
                {
                    Text = page.Markdown,
                    PageNumber = pageNumber
                });
            }

            foreach (OcrImage image in page.Images)
            {
                section.Elements.Add(MapImage(image, pageNumber));
            }

            document.Sections.Add(section);
        }

        return document;
    }

    private static IngestionDocumentImage MapImage(OcrImage image, int pageNumber)
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
            (float left, float top, float right, float bottom) = image.BoundingRegion.GetBounds();
            element.Metadata[BoundingBoxMetadataKey] = new[] { left, top, right, bottom };
            element.Metadata[BoundingRegionMetadataKey] = image.BoundingRegion.Polygon.ToArray();
        }

        return element;
    }

    private static string CreateImageMarkdown(OcrImage image)
    {
        string altText = image.Caption ?? string.Empty;
        string uri = image.Content?.Uri ?? string.Empty;

        return $"![{altText}]({uri})";
    }
}

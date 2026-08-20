// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEDE0001 // Document extraction abstractions are experimental.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

public class OcrDocumentReaderTests
{
    [Fact]
    public async Task MapsOcrImagesToIngestionDocumentImages()
    {
        byte[] imageBytes = [1, 2, 3, 4, 5];
        DocumentExtractionResult documentExtractionResult = new(
        [
            new DocumentPage(2, "Page text")
            {
                Elements =
                [
                    new DocumentImage
                    {
                        Content = new DataContent(imageBytes, "image/png"),
                        Caption = "Architecture diagram",
                        BoundingRegion = DocumentBoundingRegion.FromRectangle(3, left: 1, top: 2, right: 10, bottom: 20)
                    }
                ]
            }
        ]);
        using TestDocumentExtractionClient documentExtractionClient = new(documentExtractionResult);
        OcrDocumentReader reader = new(documentExtractionClient);

        using MemoryStream source = new([42]);
        IngestionDocument document = await reader.ReadAsync(source, "doc-id", "application/pdf");

        IngestionDocumentImage image = Assert.Single(document.EnumerateContent().OfType<IngestionDocumentImage>());
        Assert.Equal(imageBytes, image.Content?.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal("Architecture diagram", image.AlternativeText);
        Assert.Equal(3, image.PageNumber);
        Assert.Equal([1f, 2f, 10f, 20f], Assert.IsType<float[]>(image.Metadata["bounding_box"]));
        Assert.Equal([1f, 2f, 10f, 2f, 10f, 20f, 1f, 20f], Assert.IsType<float[]>(image.Metadata["bounding_region"]));
        Assert.Equal("application/pdf", documentExtractionClient.MediaType);
        Assert.NotNull(documentExtractionClient.Options);
    }

    private sealed class TestDocumentExtractionClient(DocumentExtractionResult result) : IDocumentExtractionClient
    {
        public string? MediaType { get; private set; }

        public DocumentExtractionOptions? Options { get; private set; }

        public Task<DocumentExtractionResult> ExtractAsync(
            Stream document,
            string mediaType,
            DocumentExtractionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            MediaType = mediaType;
            Options = options;
            return Task.FromResult(result);
        }

        public IAsyncEnumerable<DocumentExtractionPageResult> ExtractPagesAsync(
            Stream document,
            string mediaType,
            DocumentExtractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

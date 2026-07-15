// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001 // OCR abstractions are experimental.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

public class OcrDocumentReaderTests
{
    [Fact]
    public async Task MapsOcrImagesToIngestionDocumentImages()
    {
        byte[] imageBytes = [1, 2, 3, 4, 5];
        OcrResult ocrResult = new(
        [
            new OcrPage(2, "Page text")
            {
                Images =
                [
                    new OcrImage
                    {
                        Content = new DataContent(imageBytes, "image/png"),
                        Caption = "Architecture diagram",
                        BoundingRegion = OcrBoundingRegion.FromRectangle(3, left: 1, top: 2, right: 10, bottom: 20)
                    }
                ]
            }
        ]);
        using TestOcrClient ocrClient = new(ocrResult);
        OcrDocumentReader reader = new(ocrClient);

        using MemoryStream source = new([42]);
        IngestionDocument document = await reader.ReadAsync(source, "doc-id", "application/pdf");

        IngestionDocumentImage image = Assert.Single(document.EnumerateContent().OfType<IngestionDocumentImage>());
        Assert.Equal(imageBytes, image.Content?.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal("Architecture diagram", image.AlternativeText);
        Assert.Equal(3, image.PageNumber);
        Assert.Equal([1f, 2f, 10f, 20f], Assert.IsType<float[]>(image.Metadata["bounding_box"]));
        Assert.Equal([1f, 2f, 10f, 2f, 10f, 20f, 1f, 20f], Assert.IsType<float[]>(image.Metadata["bounding_region"]));
        Assert.Equal("application/pdf", ocrClient.MediaType);
        Assert.True(ocrClient.Options?.IncludeImages);
    }

    private sealed class TestOcrClient(OcrResult result) : IOcrClient
    {
        public string? MediaType { get; private set; }

        public OcrOptions? Options { get; private set; }

        public Task<OcrResult> ExtractAsync(
            Stream document,
            string mediaType,
            OcrOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            MediaType = mediaType;
            Options = options;
            return Task.FromResult(result);
        }

        public IAsyncEnumerable<OcrResponseUpdate> ExtractStreamingAsync(
            Stream document,
            string mediaType,
            OcrOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

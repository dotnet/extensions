// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DocumentExtractionPageResultExtensionsTests
{
    [Fact]
    public void ToDocumentExtractionResult_NullUpdates_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((IEnumerable<DocumentExtractionPageResult>)null!).ToDocumentExtractionResult());
    }

    [Fact]
    public async Task ToDocumentExtractionResultAsync_NullUpdates_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("updates", () => ((IAsyncEnumerable<DocumentExtractionPageResult>)null!).ToDocumentExtractionResultAsync());
    }

    [Fact]
    public void ToDocumentExtractionResult_AssemblesPagesAndUsage()
    {
        DocumentExtractionPageResult[] updates =
        [
            new(new DocumentPage(1, "page one")) { PagesProcessed = 1, TotalPages = 2 },
            new(new DocumentPage(2, "page two")) { Usage = new() { PagesProcessed = 2 } },
        ];

        DocumentExtractionResult result = updates.ToDocumentExtractionResult();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Text);
        Assert.NotNull(result.Usage);
        Assert.Equal(2, result.Usage!.PagesProcessed);
    }

    [Fact]
    public async Task ToDocumentExtractionResultAsync_AssemblesPagesAndUsageAsync()
    {
        DocumentExtractionPageResult[] updates =
        [
            new(new DocumentPage(1, "page one")),
            new(new DocumentPage(2, "page two")),
        ];

        DocumentExtractionResult result = await YieldAsync(updates).ToDocumentExtractionResultAsync();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Text);
    }

    [Fact]
    public void ToDocumentExtractionResult_MergesAdditionalProperties()
    {
        DocumentExtractionPageResult[] updates =
        [
            new(new DocumentPage(1, "page one")) { AdditionalProperties = new() { ["a"] = "1" } },
            new(new DocumentPage(2, "page two")) { AdditionalProperties = new() { ["b"] = "2" } },
        ];

        DocumentExtractionResult result = updates.ToDocumentExtractionResult();

        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("1", result.AdditionalProperties!["a"]);
        Assert.Equal("2", result.AdditionalProperties!["b"]);
    }

    [Fact]
    public void ToDocumentExtractionResult_PreservesPerPageCoordinateMetadata()
    {
        DocumentExtractionPageResult[] updates =
        [
            new(new DocumentPage(1, "page one") { CoordinateUnit = DocumentCoordinateUnit.Pixel, CoordinateOrigin = DocumentCoordinateOrigin.TopLeft }),
            new(new DocumentPage(2, "page two")),
            new(new DocumentPage(3, "page three") { CoordinateUnit = DocumentCoordinateUnit.Point, CoordinateOrigin = DocumentCoordinateOrigin.BottomLeft }),
        ];

        DocumentExtractionResult result = updates.ToDocumentExtractionResult();

        Assert.Collection(
            result.Pages,
            p =>
            {
                Assert.Equal(DocumentCoordinateUnit.Pixel, p.CoordinateUnit);
                Assert.Equal(DocumentCoordinateOrigin.TopLeft, p.CoordinateOrigin);
            },
            p =>
            {
                Assert.Null(p.CoordinateUnit);
                Assert.Null(p.CoordinateOrigin);
            },
            p =>
            {
                Assert.Equal(DocumentCoordinateUnit.Point, p.CoordinateUnit);
                Assert.Equal(DocumentCoordinateOrigin.BottomLeft, p.CoordinateOrigin);
            });
    }

    [Fact]
    public void ToDocumentExtractionResult_PreservesPerPageRawRepresentation()
    {
        object rawPageOne = new { page = 1 };
        object rawPageTwo = new { page = 2 };

        DocumentExtractionPageResult[] updates =
        [
            new(new DocumentPage(1, "page one") { RawRepresentation = rawPageOne }),
            new(new DocumentPage(2, "page two") { RawRepresentation = rawPageTwo }),
        ];

        DocumentExtractionResult result = updates.ToDocumentExtractionResult();

        Assert.Collection(
            result.Pages,
            p => Assert.Same(rawPageOne, p.RawRepresentation),
            p => Assert.Same(rawPageTwo, p.RawRepresentation));
    }

    private static async IAsyncEnumerable<DocumentExtractionPageResult> YieldAsync(IEnumerable<DocumentExtractionPageResult> updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}

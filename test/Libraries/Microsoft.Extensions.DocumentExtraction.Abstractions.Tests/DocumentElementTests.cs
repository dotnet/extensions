// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DocumentElementTests
{
    [Fact]
    public void Elements_OfType_ProjectsEachKindInReadingOrder()
    {
        DocumentPage page = new(1, "page text")
        {
            Elements =
            [
                new DocumentBlock("intro"),
                new DocumentTable(1, 1),
                new DocumentImage { Caption = "figure" },
                new DocumentBlock("outro"),
            ],
        };

        Assert.Equal(4, page.Elements.Count);
        Assert.Equal(["intro", "outro"], page.Elements.OfType<DocumentBlock>().Select(b => b.Text));
        Assert.Single(page.Elements.OfType<DocumentTable>());
        Assert.Equal("figure", Assert.Single(page.Elements.OfType<DocumentImage>()).Caption);
    }

    [Fact]
    public void Elements_SerializePolymorphically_RoundTrip()
    {
        DocumentExtractionResult result = new(
        [
            new DocumentPage(1, "page text")
            {
                CoordinateUnit = DocumentCoordinateUnit.Point,
                CoordinateOrigin = DocumentCoordinateOrigin.BottomLeft,
                Elements =
                [
                    new DocumentBlock("title") { Kind = DocumentBlockKind.Title, Confidence = 0.9 },
                    new DocumentTable(1, 2, [new DocumentTableCell(0, 0, "a") { Kind = DocumentTableCellKind.RowHeader }, new DocumentTableCell(0, 1, "b")]),
                    new DocumentImage { Caption = "figure", Confidence = 0.5 },
                ],
            },
        ]);

        string json = JsonSerializer.Serialize(result, AIJsonUtilities.DefaultOptions);

        Assert.Contains("$type", json);
        Assert.Contains("block", json);
        Assert.Contains("table", json);
        Assert.Contains("image", json);
        Assert.Contains("Point", json);
        Assert.Contains("BottomLeft", json);

        DocumentExtractionResult roundTripped = JsonSerializer.Deserialize<DocumentExtractionResult>(json, AIJsonUtilities.DefaultOptions)!;

        DocumentPage page = Assert.Single(roundTripped.Pages);
        Assert.Equal(DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, page.CoordinateOrigin);
        Assert.Collection(
            page.Elements,
            e => Assert.Equal("title", Assert.IsType<DocumentBlock>(e).Text),
            e => Assert.Equal(2, Assert.IsType<DocumentTable>(e).ColumnCount),
            e => Assert.Equal("figure", Assert.IsType<DocumentImage>(e).Caption));
        Assert.Equal(0.9, page.Elements.OfType<DocumentBlock>().Single().Confidence);
    }

    [Fact]
    public void TableCell_NestedElements_RoundTrip()
    {
        DocumentTableCell cell = new(0, 0, "flat text")
        {
            Elements = [new DocumentBlock("nested paragraph")],
        };

        string json = JsonSerializer.Serialize(cell, AIJsonUtilities.DefaultOptions);
        DocumentTableCell roundTripped = JsonSerializer.Deserialize<DocumentTableCell>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal("flat text", roundTripped.Content);
        Assert.NotNull(roundTripped.Elements);
        Assert.Equal("nested paragraph", Assert.IsType<DocumentBlock>(Assert.Single(roundTripped.Elements!)).Text);
    }

    [Fact]
    public void TableCell_GeometryConfidenceAndProperties_RoundTrip()
    {
        DocumentTableCell cell = new(0, 0, "flat text")
        {
            BoundingRegion = DocumentBoundingRegion.FromRectangle(1, left: 10, top: 20, right: 110, bottom: 220),
            Confidence = 0.75,
            RawRepresentation = new { ignored = true },
            AdditionalProperties = new() { ["detectedLanguages"] = "en" },
        };

        string json = JsonSerializer.Serialize(cell, AIJsonUtilities.DefaultOptions);
        DocumentTableCell roundTripped = JsonSerializer.Deserialize<DocumentTableCell>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.NotNull(roundTripped.BoundingRegion);
        Assert.Equal(1, roundTripped.BoundingRegion!.PageNumber);
        Assert.Equal(0.75, roundTripped.Confidence);
        Assert.NotNull(roundTripped.AdditionalProperties);
        Assert.True(roundTripped.AdditionalProperties!.ContainsKey("detectedLanguages"));
        Assert.Null(roundTripped.RawRepresentation);
    }
}

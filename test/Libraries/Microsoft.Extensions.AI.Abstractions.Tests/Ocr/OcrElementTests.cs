// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrElementTests
{
    [Fact]
    public void Elements_OfType_ProjectsEachKindInReadingOrder()
    {
        OcrPage page = new(1, "page text")
        {
            Elements =
            [
                new OcrBlock("intro"),
                new OcrTable(1, 1),
                new OcrImage { Caption = "figure" },
                new OcrBlock("outro"),
            ],
        };

        Assert.Equal(4, page.Elements.Count);
        Assert.Equal(["intro", "outro"], page.Elements.OfType<OcrBlock>().Select(b => b.Text));
        Assert.Single(page.Elements.OfType<OcrTable>());
        Assert.Equal("figure", Assert.Single(page.Elements.OfType<OcrImage>()).Caption);
    }

    [Fact]
    public void Elements_SerializePolymorphically_RoundTrip()
    {
        OcrResult result = new(
        [
            new OcrPage(1, "page text")
            {
                CoordinateUnit = OcrCoordinateUnit.Point,
                CoordinateOrigin = OcrCoordinateOrigin.BottomLeft,
                Elements =
                [
                    new OcrBlock("title") { Kind = OcrBlockKind.Title, Confidence = 0.9 },
                    new OcrTable(1, 2, [new OcrTableCell(0, 0, "a") { Kind = OcrTableCellKind.RowHeader }, new OcrTableCell(0, 1, "b")]),
                    new OcrImage { Caption = "figure", Confidence = 0.5 },
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

        OcrResult roundTripped = JsonSerializer.Deserialize<OcrResult>(json, AIJsonUtilities.DefaultOptions)!;

        OcrPage page = Assert.Single(roundTripped.Pages);
        Assert.Equal(OcrCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(OcrCoordinateOrigin.BottomLeft, page.CoordinateOrigin);
        Assert.Collection(
            page.Elements,
            e => Assert.Equal("title", Assert.IsType<OcrBlock>(e).Text),
            e => Assert.Equal(2, Assert.IsType<OcrTable>(e).ColumnCount),
            e => Assert.Equal("figure", Assert.IsType<OcrImage>(e).Caption));
        Assert.Equal(0.9, page.Elements.OfType<OcrBlock>().Single().Confidence);
    }

    [Fact]
    public void TableCell_NestedElements_RoundTrip()
    {
        OcrTableCell cell = new(0, 0, "flat text")
        {
            Elements = [new OcrBlock("nested paragraph")],
        };

        string json = JsonSerializer.Serialize(cell, AIJsonUtilities.DefaultOptions);
        OcrTableCell roundTripped = JsonSerializer.Deserialize<OcrTableCell>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal("flat text", roundTripped.Content);
        Assert.NotNull(roundTripped.Elements);
        Assert.Equal("nested paragraph", Assert.IsType<OcrBlock>(Assert.Single(roundTripped.Elements!)).Text);
    }

    [Fact]
    public void TableCell_GeometryConfidenceAndProperties_RoundTrip()
    {
        OcrTableCell cell = new(0, 0, "flat text")
        {
            BoundingRegion = OcrBoundingRegion.FromRectangle(1, left: 10, top: 20, right: 110, bottom: 220),
            Confidence = 0.75,
            RawRepresentation = new { ignored = true },
            AdditionalProperties = new() { ["detectedLanguages"] = "en" },
        };

        string json = JsonSerializer.Serialize(cell, AIJsonUtilities.DefaultOptions);
        OcrTableCell roundTripped = JsonSerializer.Deserialize<OcrTableCell>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.NotNull(roundTripped.BoundingRegion);
        Assert.Equal(1, roundTripped.BoundingRegion!.PageNumber);
        Assert.Equal(0.75, roundTripped.Confidence);
        Assert.NotNull(roundTripped.AdditionalProperties);
        Assert.True(roundTripped.AdditionalProperties!.ContainsKey("detectedLanguages"));
        Assert.Null(roundTripped.RawRepresentation);
    }
}

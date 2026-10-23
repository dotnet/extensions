// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTableCellTests
{
    [Fact]
    public void HasRowColumnAndStructuralAddress()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 2, 2);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");

        table.Cells.SetContent(1, 0, content);
        section.Elements.Add(table);
        document.Sections.Add(section);

        DocumentTableCell cell = table.Cells[1, 0]!;

        Assert.Equal(1, cell.RowIndex);
        Assert.Equal(0, cell.ColumnIndex);
        Assert.Equal("s0/e0", table.Address);
        Assert.Equal("s0/e0/c1,0", cell.Address);
        Assert.Equal("s0/e0/c1,0", content.Address);
        Assert.Null(content.ParentSection);
    }

    [Fact]
    public void Address_ChangesWithTableStructure()
    {
        using Document document = new("document");
        DocumentSection firstSection = new("first");
        DocumentSection secondSection = new("second");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");

        table.Cells.SetContent(0, 0, content);
        firstSection.Elements.Add(table);
        document.Sections.Add(firstSection);
        document.Sections.Add(secondSection);

        DocumentTableCell cell = table.Cells[0, 0]!;
        Assert.Equal("s0/e0", table.Address);
        Assert.Equal("s0/e0/c0,0", cell.Address);
        Assert.Equal("s0/e0/c0,0", content.Address);

        firstSection.Elements.Remove(table);

        Assert.Null(table.Address);
        Assert.Null(cell.Address);
        Assert.Null(content.Address);

        secondSection.Elements.Add(table);

        Assert.Equal("s1/e0", table.Address);
        Assert.Equal("s1/e0/c0,0", cell.Address);
        Assert.Equal("s1/e0/c0,0", content.Address);
    }

    [Fact]
    public void Constructor_RejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(
            "content",
            () => new DocumentTableCell(null!));
    }

    [Fact]
    public void EmptyCell_PreservesGeometryAndConfidenceWithoutText()
    {
        using Document document = new("empty-cell");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        DocumentTableCell empty = new();
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        empty.Annotations.Add(new DocumentAnnotation("empty-layout",
            regions: new[] { new DocumentRegion(page, DocumentAnnotationTestData.Polygon()) },
            confidence: new DocumentConfidence(0.73, "provider-reported symbol certainty", 0, 1)));
        table.Cells[2, 2] = empty;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Null(empty.Content);
        Assert.Equal("s0/e0/c2,2", empty.Address);
        Assert.Equal(2, empty.RowIndex);
        Assert.Equal(2, empty.ColumnIndex);
        Assert.Equal(1, empty.RowSpan);
        Assert.Equal(1, empty.ColumnSpan);
        Assert.False(table.Cells.IsCovered(2, 2));
        Assert.Same(empty, table.Cells.GetCoveringCell(2, 2));
        Assert.Null(table.Cells.GetCoveringCell(1, 2));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), empty.Annotations[0].Regions[0].Polygon);
        Assert.Equal(0.73, empty.Annotations[0].Confidence!.Value);
        Assert.Equal("provider-reported symbol certainty", empty.Annotations[0].Confidence!.Kind);
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Equal($"|  |  |  |{Environment.NewLine}|  |  |  |{Environment.NewLine}|  |  |  |", document.ToMarkdown());
        empty.Annotations.Add(new DocumentAnnotation("additional"));
        Assert.Equal(version, document.Version);
        Assert.Equal(2, empty.Annotations.Count);
    }

    [Theory]
    [InlineData(0, 1, "rowSpan")]
    [InlineData(-1, 1, "rowSpan")]
    [InlineData(1, 0, "columnSpan")]
    [InlineData(1, -1, "columnSpan")]
    public void Constructors_RejectNonPositiveSpans(int rowSpan, int columnSpan, string parameter)
    {
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "content");
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => new DocumentTableCell(rowSpan, columnSpan));
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => new DocumentTableCell(text, rowSpan, columnSpan));
        Assert.Null(text.Address);
        Assert.Null(text.ParentSection);
        Assert.Equal("content", text.ToMarkdown());
    }

    [Fact]
    public void CellFrames_ResolveMultipleCoordinateSystemsAfterTableReparenting()
    {
        using Document document = new("multi-frame-cell");
        DocumentSourceOutput output = new("operation-A");
        DocumentSourcePage pdf = DocumentAnnotationTestData.PdfPage(output);
        DocumentSourcePage raster = new("raster-frame", 7, new DocumentPageDimensions(400, 200),
            DocumentCoordinateUnit.Pixel, DocumentCoordinateOrigin.TopLeft, output);
        DocumentPoint[] rasterPolygon =
        [
            new(20.5, 159), new(81.5, 163.5), new(89, 79.75), new(25.75, 82.5), new(17, 129.5)
        ];
        DocumentTableCell empty = new();
        empty.Annotations.Add(new DocumentAnnotation("dual-layout", role: "vendor-empty",
            regions: new[]
            {
                new DocumentRegion(pdf, DocumentAnnotationTestData.Polygon()),
                new DocumentRegion(raster, rasterPolygon)
            }));
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "lO0  ??", "lO0  ??");
        DocumentTableCell ordinary = new(text);
        text.Annotations.Add(new DocumentAnnotation("opaque-score",
            confidence: new DocumentConfidence(-12.5, "vendor unknown margin")));
        DocumentTableElement table = new("stored table", 1, 3);
        table.Cells[0, 1] = empty;
        table.Cells[0, 2] = ordinary;
        DocumentSection first = new("");
        DocumentSection second = new("");
        first.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "first"));
        second.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "second"));
        first.Elements.Add(table);
        document.Sections.Add(first);
        document.Sections.Add(second);
        long version = document.Version;

        Assert.Equal("s0/e1/c0,1", empty.Address);
        Assert.Equal("s0/e1/c0,2", text.Address);
        Assert.Equal($"first{Environment.NewLine}|  |  | lO0  ?? |{Environment.NewLine}second", document.ToMarkdown());
        Assert.Equal(new[] { pdf, raster }, document.SourcePages);
        Assert.Same(output, Assert.Single(document.SourceOutputs));

        Assert.True(first.Elements.Remove(table));
        Assert.Null(table.Address);
        Assert.Null(empty.Address);
        Assert.Null(text.Address);
        Assert.Equal(new[] { pdf, raster }, document.SourcePages);
        second.Elements.Add(table);

        Assert.Equal(version + 2, document.Version);
        Assert.Equal("s1/e1", table.Address);
        Assert.Equal("s1/e1/c0,1", empty.Address);
        Assert.Equal("s1/e1/c0,2", ordinary.Address);
        Assert.Equal("s1/e1/c0,2", text.Address);
        Assert.Null(empty.Content);
        Assert.Same(empty, table.Cells.GetCoveringCell(0, 1));
        Assert.False(table.Cells.IsCovered(0, 1));
        Assert.Null(table.Cells.GetCoveringCell(0, 0));
        Assert.Equal(new[] { pdf, raster }, document.SourcePages);
        DocumentAnnotation layout = Assert.Single(table.Cells.GetCoveringCell(0, 1)!.Annotations);
        Assert.Equal("dual-layout", layout.Kind);
        Assert.Equal("vendor-empty", layout.Role);
        Assert.Null(layout.Confidence);
        Assert.Equal(2, layout.Regions.Count);
        Assert.Same(document.SourcePages[0], layout.Regions[0].Page);
        Assert.Same(document.SourcePages[1], layout.Regions[1].Page);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), layout.Regions[0].Polygon);
        Assert.Equal(rasterPolygon, layout.Regions[1].Polygon);
        Assert.Equal(7, layout.Regions[0].Page.PageNumber);
        Assert.Equal(7, layout.Regions[1].Page.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), layout.Regions[0].Page.Dimensions);
        Assert.Equal(new DocumentPageDimensions(400, 200), layout.Regions[1].Page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, layout.Regions[0].Page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, layout.Regions[0].Page.CoordinateOrigin);
        Assert.Equal(DocumentCoordinateUnit.Pixel, layout.Regions[1].Page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.TopLeft, layout.Regions[1].Page.CoordinateOrigin);
        Assert.Same(output, layout.Regions[0].Page.SourceOutput);
        Assert.Same(output, layout.Regions[1].Page.SourceOutput);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        DocumentAnnotation score = Assert.Single(table.Cells.GetCoveringCell(0, 2)!.Content!.Annotations);
        Assert.Empty(score.Regions);
        Assert.Equal(-12.5, score.Confidence!.Value);
        Assert.Equal("vendor unknown margin", score.Confidence.Kind);
        Assert.Null(score.Confidence.Minimum);
        Assert.Null(score.Confidence.Maximum);
        Assert.Equal("lO0  ??", text.Text);
        Assert.Equal("lO0  ??", text.GetMarkdown());
        Assert.Equal("stored table", table.GetMarkdown());
        Assert.Equal($"first{Environment.NewLine}second{Environment.NewLine}|  |  | lO0  ?? |", document.ToMarkdown());
        Assert.Equal(version + 2, document.Version);
    }
}

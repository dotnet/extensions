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
    public void EmptyCell_PreservesGeometryWithoutText()
    {
        using Document document = new("empty-cell");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        DocumentTableCell empty = new();
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        empty.Sources.Add(new DocumentSourceReference(page, DocumentAnnotationTestData.Polygon()));
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
        Assert.Equal(DocumentAnnotationTestData.Polygon(), empty.Sources[0].Polygon);
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Equal($"|  |  |  |{Environment.NewLine}|  |  |  |{Environment.NewLine}|  |  |  |", document.ToMarkdown());
        empty.Sources.Add(new DocumentSourceReference(page));
        Assert.Equal(version, document.Version);
        Assert.Equal(2, empty.Sources.Count);
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
        empty.Sources.Add(new DocumentSourceReference(pdf, DocumentAnnotationTestData.Polygon()));
        empty.Sources.Add(new DocumentSourceReference(raster, rasterPolygon));
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "lO0  ??", "lO0  ??");
        DocumentTableCell ordinary = new(text);
        text.Metadata["vendor unknown margin"] = -12.5;
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
        var sources = table.Cells.GetCoveringCell(0, 1)!.Sources;
        Assert.Equal(2, sources.Count);
        Assert.Same(document.SourcePages[0], sources[0].Page);
        Assert.Same(document.SourcePages[1], sources[1].Page);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), sources[0].Polygon);
        Assert.Equal(rasterPolygon, sources[1].Polygon);
        Assert.Equal(7, sources[0].Page.PageNumber);
        Assert.Equal(7, sources[1].Page.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), sources[0].Page.Dimensions);
        Assert.Equal(new DocumentPageDimensions(400, 200), sources[1].Page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, sources[0].Page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, sources[0].Page.CoordinateOrigin);
        Assert.Equal(DocumentCoordinateUnit.Pixel, sources[1].Page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.TopLeft, sources[1].Page.CoordinateOrigin);
        Assert.Same(output, sources[0].Page.SourceOutput);
        Assert.Same(output, sources[1].Page.SourceOutput);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Empty(table.Cells.GetCoveringCell(0, 2)!.Content!.Sources);
        Assert.Equal(-12.5, text.Metadata["vendor unknown margin"]);
        Assert.Equal("lO0  ??", text.Text);
        Assert.Equal("lO0  ??", text.GetMarkdown());
        Assert.Equal("stored table", table.GetMarkdown());
        Assert.Equal($"first{Environment.NewLine}second{Environment.NewLine}|  |  | lO0  ?? |", document.ToMarkdown());
        Assert.Equal(version + 2, document.Version);
    }
}

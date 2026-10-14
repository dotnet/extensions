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
}

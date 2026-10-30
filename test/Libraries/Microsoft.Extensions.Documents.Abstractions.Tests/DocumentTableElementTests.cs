// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTableElementTests
{
    [Fact]
    public void Coverage_TracksSpanReplacementRemovalAndRejectedEdits()
    {
        using Document document = new("coverage");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 4);
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentTableCell wide = new(new DocumentTextElement("wide"), 2, 3);
        DocumentTableCell corner = new(new DocumentTextElement("corner"), role: "rowHeader");
        table.Cells[0, 0] = wide;
        table.Cells[2, 3] = corner;
        AssertCoverage(wide, 2, 3);
        long version = document.Version;

        Assert.Throws<InvalidOperationException>(() => table.Cells[1, 1] = new DocumentTableCell());
        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 3] = new DocumentTableCell(3, 1));
        Assert.Throws<ArgumentException>(() => table.Cells[0, 0] = new DocumentTableCell(4, 1));
        Assert.Equal(version, document.Version);
        AssertCoverage(wide, 2, 3);

        DocumentTableCell small = new(new DocumentTextElement("small"));
        table.Cells[0, 0] = small;
        AssertCoverage(small, 1, 1);
        Assert.Null(wide.Content!.Address);
        Assert.Equal("s0/e0/c0,0", small.Content!.Address);
        table.Cells[0, 0] = null;
        AssertCoverage(null, 0, 0);
        Assert.Null(small.Content.Address);
        table.Cells[0, 0] = wide;
        AssertCoverage(wide, 2, 3);
        Assert.Equal("s0/e0/c0,0", wide.Content.Address);
        Assert.Equal(
            "<table><tr><td rowspan=\"2\" colspan=\"3\">wide</td><td></td></tr>" +
            "<tr><td></td></tr><tr><td></td><td></td><td></td>" +
            "<th rowspan=\"1\" colspan=\"1\" data-kind=\"rowHeader\">corner</th></tr></table>",
            table.ToMarkdown());

        void AssertCoverage(DocumentTableCell? anchor, int rows, int columns)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    bool inSpan = row < rows && column < columns;
                    DocumentTableCell? cornerCell = row == 2 && column == 3 ? corner : null;
                    DocumentTableCell? expected = inSpan ? anchor : cornerCell;
                    Assert.Same(expected, table.Cells.GetCoveringCell(row, column));
                    Assert.Equal(inSpan && (row != 0 || column != 0),
                        table.Cells.IsCovered(row, column));
                    Assert.Same(row == 0 && column == 0 ? anchor : cornerCell,
                        table.Cells[row, column]);
                }
            }
        }
    }

    private const string RowSpanProjection =
        "<table><tr><td rowspan=\"1\" colspan=\"1\"></td><td></td><td></td><td></td></tr>" +
        "<tr><td></td><td rowspan=\"2\" colspan=\"1\">A|&lt;</td><td></td><td></td></tr>" +
        "<tr><td></td><td></td><td></td></tr></table>";

    private const string ColumnSpanProjection =
        "<table><tr><td rowspan=\"1\" colspan=\"1\"></td><td></td><td></td><td></td></tr>" +
        "<tr><td></td><td rowspan=\"1\" colspan=\"3\">A|&lt;</td></tr>" +
        "<tr><td></td><td></td><td></td><td></td></tr></table>";

    [Fact]
    public void StoresDimensionsAndCreatesNullableCells()
    {
        DocumentTableElement table = new("stored table", 2, 3);

        Assert.Equal("stored table", table.Markdown);
        Assert.Equal(2, table.Cells.RowCount);
        Assert.Equal(3, table.Cells.ColumnCount);
        Assert.Equal(
            new DocumentTableCell?[] { null, null, null, null, null, null },
            table.Cells.ToArray());
    }

    [Theory]
    [InlineData(-1, 0, "rowCount")]
    [InlineData(0, -1, "columnCount")]
    public void RejectsNegativeDimensions(int rowCount, int columnCount, string parameterName)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            parameterName,
            () => new DocumentTableElement("stored table", rowCount, columnCount));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void ZeroDimensions_ToMarkdownReturnsMarkdown(int rowCount, int columnCount)
    {
        DocumentTableElement table = new("stored table", rowCount, columnCount);

        Assert.Equal("stored table", table.ToMarkdown());
    }

    [Theory]
    [InlineData(-1, 0, "row")]
    [InlineData(2, 0, "row")]
    [InlineData(0, -1, "column")]
    [InlineData(0, 2, "column")]
    public void RejectsInvalidCellAddresses(int row, int column, string parameterName)
    {
        DocumentTableElement table = new("stored table", 2, 2);
        DocumentTextElement text = new("candidate");
        DocumentTableCell candidate = new(text);

        Assert.Throws<ArgumentOutOfRangeException>(
            parameterName,
            () => _ = table.Cells[row, column]);
        Assert.Throws<ArgumentOutOfRangeException>(
            parameterName,
            () => table.Cells[row, column] = candidate);
        Assert.Throws<ArgumentOutOfRangeException>(
            parameterName,
            () => table.Cells[row, column] = null);

        Assert.Null(candidate.Table);
        Assert.Null(text.OwningDocument);
        Assert.Null(text.Parent);
        Assert.Equal("candidate", text.ToMarkdown());
        Assert.Equal(
            new DocumentTableCell?[] { null, null, null, null },
            table.Cells.ToArray());
    }

    [Fact]
    public void ToMarkdown_ProjectsEveryCellAndUsesEmptyValueForNull()
    {
        DocumentTableElement table = new("stored table", 2, 3);
        table.Cells[0, 1] = new DocumentTableCell(new DocumentTextElement("one"));
        table.Cells[1, 0] = new DocumentTableCell(new DocumentTextElement("two"));

        Assert.Equal(
            $"|  | one |  |{Environment.NewLine}| two |  |  |",
            table.ToMarkdown());
    }

    [Fact]
    public void ToMarkdown_EscapesPipesInCellProjections()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        table.Cells[0, 0] = new DocumentTableCell(new DocumentTextElement("left|right"));

        Assert.Equal("| left\\|right |", table.ToMarkdown());
    }

    [Fact]
    public void Markdown_RemainsSuppliedWhileToMarkdownProjectsCells()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        table.Cells[0, 0] = new DocumentTableCell(new DocumentTextElement("semantic cell"));

        Assert.Equal("stored table", table.Markdown);
        Assert.Equal("| semantic cell |", table.ToMarkdown());
    }

    [Fact]
    public void MergedCells_ProjectAnchorContentExactlyOnceWithRolesAndHtmlEscaping()
    {
        DocumentTableElement table = new("stored table", 3, 3);
        DocumentTableCell anchor = new(new DocumentTextElement("MERGED_A<&"), 2, 2, "columnHeader");
        table.Cells[0, 0] = anchor;
        table.Cells[2, 0] = new DocumentTableCell(new DocumentTextElement("B"), role: "rowHeader");
        DocumentTableCell empty = new(role: "vendor\"role");
        table.Cells[2, 2] = empty;
        DocumentTableCell?[] before = table.Cells.ToArray();

        Assert.Equal(
            "<table><tr><th rowspan=\"2\" colspan=\"2\" data-kind=\"columnHeader\">MERGED_A&lt;&amp;</th><td></td></tr>" +
            "<tr><td></td></tr><tr><th rowspan=\"1\" colspan=\"1\" data-kind=\"rowHeader\">B</th><td></td>" +
            "<td rowspan=\"1\" colspan=\"1\" data-kind=\"vendor&quot;role\"></td></tr></table>",
            table.ToMarkdown());
        Assert.Equal(before, table.Cells.ToArray());
        Assert.Same(anchor, table.Cells.GetCoveringCell(1, 1));
        Assert.Null(table.Cells[1, 1]);
        Assert.False(table.Cells.IsCovered(2, 2));
        Assert.Null(empty.Content);
        Assert.Equal("stored table", table.Markdown);
    }

    [Theory]
    [InlineData(null, "|  INV0ICE  1O0? <&\\|  |")]
    [InlineData("columnHeader", "<table><tr><th rowspan=\"1\" colspan=\"1\" data-kind=\"columnHeader\"> INV0ICE  1O0? &lt;&amp;| </th></tr></table>")]
    [InlineData("rowHeader", "<table><tr><th rowspan=\"1\" colspan=\"1\" data-kind=\"rowHeader\"> INV0ICE  1O0? &lt;&amp;| </th></tr></table>")]
    [InlineData("ColumnHeader", "<table><tr><td rowspan=\"1\" colspan=\"1\" data-kind=\"ColumnHeader\"> INV0ICE  1O0? &lt;&amp;| </td></tr></table>")]
    [InlineData("header", "<table><tr><td rowspan=\"1\" colspan=\"1\" data-kind=\"header\"> INV0ICE  1O0? &lt;&amp;| </td></tr></table>")]
    [InlineData("vendor\"role", "<table><tr><td rowspan=\"1\" colspan=\"1\" data-kind=\"vendor&quot;role\"> INV0ICE  1O0? &lt;&amp;| </td></tr></table>")]
    public void ToMarkdown_UsesOnlyExplicitCellRolesWithoutCorrectingTranscription(string? role, string expected)
    {
        using Document document = new("explicit-roles", "stored document");
        DocumentSection section = new("stored section");
        DocumentTableElement table = new("stored imperfect table", 1, 1);

        // Header text is not permission to invent a table-cell role or repair recognition.
        DocumentTextElement text = new(" INV0ICE  1O0? <&| ", " INV0ICE  1O0? <&| ", role: "heading");
        DocumentTableCell cell = new(text, role: role);
        table.Cells[0, 0] = cell;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal(expected, document.ToMarkdown());
        Assert.Equal(expected, table.ToMarkdown());
        Assert.Equal("stored imperfect table", table.Markdown);
        Assert.Equal("stored document", document.Markdown);
        Assert.Equal(" INV0ICE  1O0? <&| ", text.Markdown);
        Assert.Equal(" INV0ICE  1O0? <&| ", text.Text);
        Assert.Equal(role, cell.Role);
        Assert.Same(cell, table.Cells.GetCoveringCell(0, 0));
        Assert.False(table.Cells.IsCovered(0, 0));
        Assert.Equal("s0/e0/c0,0", text.Address);
        Assert.Equal(version, document.Version);
    }

    [Theory]
    [InlineData(2, 1, RowSpanProjection)]
    [InlineData(1, 3, ColumnSpanProjection)]
    public void ToMarkdown_NonOriginSingleAxisSpansRespectDeclaredGrid(int rowSpan, int columnSpan, string expected)
    {
        using Document document = new("single-axis");
        DocumentSection section = new("");
        DocumentTableElement table = new("stored grid", 3, 4);
        DocumentTableCell empty = new();
        DocumentTextElement text = new("A|<", "A|<");
        DocumentTableCell anchor = new(text, rowSpan, columnSpan);
        table.Cells[0, 0] = empty;
        table.Cells[1, 1] = anchor;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal(expected, document.ToMarkdown());
        Assert.Equal(3, table.Cells.RowCount);
        Assert.Equal(4, table.Cells.ColumnCount);
        Assert.Equal(1, anchor.RowIndex);
        Assert.Equal(1, anchor.ColumnIndex);
        Assert.Equal(rowSpan, anchor.RowSpan);
        Assert.Equal(columnSpan, anchor.ColumnSpan);
        Assert.Equal("s0/e0/c1,1", anchor.Address);
        Assert.Equal("s0/e0/c1,1", text.Address);
        Assert.Equal("A|<", text.Text);
        Assert.Null(anchor.Role);
        Assert.Equal(
            new DocumentTableCell?[] { empty, null, null, null, null, anchor, null, null, null, null, null, null },
            table.Cells.ToArray());
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                bool inSpan = row >= 1 && row < 1 + rowSpan && column >= 1 && column < 1 + columnSpan;
                DocumentTableCell? covering = inSpan ? anchor : null;
                if (row == 0 && column == 0)
                {
                    covering = empty;
                }

                Assert.Same(covering, table.Cells.GetCoveringCell(row, column));
                Assert.Equal(inSpan && (row != 1 || column != 1), table.Cells.IsCovered(row, column));
            }
        }

        Assert.Equal("stored grid", table.Markdown);
        Assert.Same(table, Assert.Single(document.EnumerateContent()));
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void PipeProjection_DoesNotSerializeEmptyCellSources()
    {
        using Document document = new("projection-loss", "stored document");
        DocumentSection section = new("");
        DocumentTableElement table = new("stored source table", 1, 3);
        DocumentTableCell empty = new();
        DocumentSourcePage frame = DocumentAnnotationTestData.PdfPage();
        empty.Sources.Add(new DocumentSourceReference(frame, DocumentAnnotationTestData.Polygon(), "NATIVE_ONLY"));
        DocumentTextElement text = new("INV0ICE  1O0?", "INV0ICE  1O0?");
        table.Cells[0, 1] = empty;
        table.Cells[0, 2] = new DocumentTableCell(text);
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal("|  |  | INV0ICE  1O0? |", document.ToMarkdown());
        Assert.Equal(version, document.Version);
        Assert.Null(table.Cells[0, 0]);
        Assert.Null(table.Cells.GetCoveringCell(0, 0));
        Assert.False(table.Cells.IsCovered(0, 0));
        Assert.Same(empty, table.Cells.GetCoveringCell(0, 1));
        Assert.False(table.Cells.IsCovered(0, 1));
        Assert.Null(empty.Content);
        Assert.Equal("s0/e0/c0,1", empty.Address);
        Assert.Same(frame, Assert.Single(empty.Sources).Page);
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), empty.Sources[0].Polygon);
        Assert.Equal("NATIVE_ONLY", empty.Sources[0].GetRawRepresentation());
        Assert.Equal("INV0ICE  1O0?", text.Text);
        Assert.Equal("stored source table", table.Markdown);
        Assert.Equal("stored document", document.Markdown);

        // Structurally different tables can have the same semantic projection; this is not a wire format.
        table.Cells[0, 1] = null;

        Assert.Equal("|  |  | INV0ICE  1O0? |", document.ToMarkdown());
        Assert.Null(table.Cells[0, 1]);
        Assert.Null(table.Cells.GetCoveringCell(0, 1));
        Assert.Null(empty.Address);
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Equal(version + 1, document.Version);
        Assert.Equal("s0/e0/c0,2", text.Address);
    }

    [Fact]
    public void MarkdownViews_NoSuppliedCellsRetainSourceAndProjectEmptyGrid()
    {
        string providerMarkdown = $"|A|B|{Environment.NewLine}|---|---|{Environment.NewLine}|1O0?|B|";
        using Document document = new("unstructured-table");
        DocumentSection section = new("");
        DocumentTableElement table = new(providerMarkdown, 2, 2);
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal(2, table.Cells.RowCount);
        Assert.Equal(2, table.Cells.ColumnCount);
        Assert.Equal(new DocumentTableCell?[] { null, null, null, null }, table.Cells.ToArray());
        Assert.Null(table.Cells.GetCoveringCell(0, 0));
        Assert.False(table.Cells.IsCovered(0, 0));
        Assert.Same(table, Assert.Single(document.EnumerateContent()));
        Assert.Equal("s0/e0", table.Address);
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", table.ToMarkdown());
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        Assert.Equal(providerMarkdown, table.Markdown);
        Assert.Null(document.Markdown);
        Assert.Equal(new DocumentTableCell?[] { null, null, null, null }, table.Cells.ToArray());
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void MarkdownViews_ExplicitEmptyCellRetainsSourceAndProjectsEmptyGrid()
    {
        string providerMarkdown = $"|A|B|{Environment.NewLine}|---|---|{Environment.NewLine}|1O0?|B|";
        using Document document = new("explicit-empty-table");
        DocumentSection section = new("");
        DocumentTableElement table = new(providerMarkdown, 2, 2);
        DocumentTableCell empty = new();
        table.Cells[1, 1] = empty;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", table.ToMarkdown());
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        Assert.Equal(providerMarkdown, table.Markdown);
        Assert.Null(document.Markdown);
        Assert.Equal(new DocumentTableCell?[] { null, null, null, empty }, table.Cells.ToArray());
        Assert.Null(empty.Content);
        Assert.Same(empty, table.Cells.GetCoveringCell(1, 1));
        Assert.False(table.Cells.IsCovered(1, 1));
        Assert.Null(table.Cells.GetCoveringCell(0, 0));
        Assert.Equal("s0/e0/c1,1", empty.Address);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void MarkdownViews_RemovingAllCellsRetainsSourceAndProjectsCurrentEmptyGrid()
    {
        string providerMarkdown = $"|A|B|{Environment.NewLine}|---|---|{Environment.NewLine}|1O0?|B|";
        using Document document = new("cleared-table");
        DocumentSection section = new("");
        DocumentTableElement table = new(providerMarkdown, 2, 2);
        DocumentTextElement text = new("mapped A", "mapped A");
        DocumentTableCell populated = new(text);
        DocumentTableCell empty = new();
        table.Cells[0, 0] = populated;
        table.Cells[1, 1] = empty;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        Assert.Equal($"| mapped A |  |{Environment.NewLine}|  |  |", table.ToMarkdown());
        Assert.Equal($"| mapped A |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        Assert.Equal("s0/e0/c0,0", populated.Address);
        Assert.Equal("s0/e0/c0,0", text.Address);
        Assert.Equal("s0/e0/c1,1", empty.Address);
        Assert.Equal(providerMarkdown, table.Markdown);
        Assert.Equal(version, document.Version);

        table.Cells[0, 0] = null;

        Assert.Equal(version + 1, document.Version);
        Assert.Null(populated.Address);
        Assert.Null(text.Address);
        Assert.Same(empty, table.Cells.GetCoveringCell(1, 1));
        Assert.Equal("s0/e0/c1,1", empty.Address);
        table.Cells[1, 1] = null;

        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", table.ToMarkdown());
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        Assert.Equal(providerMarkdown, table.Markdown);
        Assert.Null(document.Markdown);
        Assert.Equal(new DocumentTableCell?[] { null, null, null, null }, table.Cells.ToArray());
        Assert.Equal(2, table.Cells.RowCount);
        Assert.Equal(2, table.Cells.ColumnCount);
        Assert.Null(empty.Address);
        Assert.Equal("mapped A", text.Text);
        Assert.Equal("mapped A", text.Markdown);
        Assert.Same(table, Assert.Single(document.EnumerateContent()));
        Assert.Equal("s0/e0", table.Address);
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                Assert.Null(table.Cells.GetCoveringCell(row, column));
                Assert.False(table.Cells.IsCovered(row, column));
            }
        }

        Assert.Equal(version + 2, document.Version);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTraversalTests
{
    private static readonly string[] _expectedBlocks = ["Title", "Intro", "Name", "Value", "Alpha", "1", "Chart label"];

    [Fact]
    public void SectionEnumerateContent_FlattensNestedSectionsWithoutYieldingSectionsOrCells()
    {
        ExtractionDocumentTextElement first = Text("first");
        ExtractionDocumentTextElement nested = Text("nested");
        ExtractionDocumentTextElement deepest = Text("deepest");
        ExtractionDocumentTableElement table = new(null, 1, 1);
        table.Cells[0, 0] = new(Text("cell"));
        ExtractionDocumentSection inner = new(new TextContent("inner local"));
        inner.Elements.Add(deepest);
        ExtractionDocumentSection middle = new();
        middle.Elements.AddRange([nested, inner, table]);
        ExtractionDocumentSection root = new();
        root.Elements.AddRange([first, middle]);

        Assert.Equal(new ExtractionDocumentElement[] { first, nested, deepest, table }, root.EnumerateContent());
        Assert.Equal(new ExtractionDocumentElement[] { nested, deepest, table }, middle.EnumerateContent());
        Assert.Empty(new ExtractionDocumentSection().EnumerateContent());
    }

    [Fact]
    public void SectionEnumerateContent_EnumerationsAreIndependentAndRejectCyclesAndNulls()
    {
        ExtractionDocumentSection root = new();
        ExtractionDocumentSection child = new();
        ExtractionDocumentTextElement text = Text("value");
        child.Elements.Add(text);
        root.Elements.AddRange([child, child]);
        IEnumerable<ExtractionDocumentElement> content = root.EnumerateContent();

        using (IEnumerator<ExtractionDocumentElement> outer = content.GetEnumerator())
        {
            Assert.True(outer.MoveNext());
            Assert.Equal(new ExtractionDocumentElement[] { text, text }, content);
            Assert.True(outer.MoveNext());
            Assert.False(outer.MoveNext());
        }

        child.Elements.Add(root);
        Assert.Throws<InvalidOperationException>(() => root.EnumerateContent().ToArray());
        Assert.Throws<InvalidOperationException>(() => child.EnumerateContent().ToArray());

        child.Elements[1] = null!;
        Assert.Throws<InvalidOperationException>(() => root.EnumerateContent().ToArray());
    }

    [Fact]
    public void Cells_EnumerateExplicitAnchorsOnlyAndGetCoveringCellResolvesEveryPosition()
    {
        ExtractionDocumentTableElement table = new(null, 3, 3);
        ExtractionDocumentTableCell header = new(Text("header"), columnSpan: 3, role: "columnHeader");
        ExtractionDocumentTableCell tall = new(Text("tall"), rowSpan: 2);
        ExtractionDocumentTableCell emptyAnchor = new();
        table.Cells[2, 2] = emptyAnchor;
        table.Cells[1, 0] = tall;
        table.Cells[0, 0] = header;

        Assert.Equal(new[] { header, tall, emptyAnchor }, table.Cells);
        Assert.Equal(
            new ExtractionDocumentTableCell?[] { header, header, header, tall, null, null, tall, null, emptyAnchor },
            Positions(table).Select(position => table.Cells.GetCoveringCell(position.row, position.column)));
        Assert.Equal(
            new ExtractionDocumentTableCell?[] { header, null, null, tall, null, null, null, null, emptyAnchor },
            Positions(table).Select(position => table.Cells[position.row, position.column]));
    }

    [Fact]
    public void GetText_ReadsEveryTextLocationInPreOrder()
    {
        ExtractionDocument document = new("all-locations", new TextContent("document local"));
        ExtractionDocumentSection page = new(new TextContent("section local"));
        ExtractionDocumentSection nested = new();
        nested.Elements.Add(Text("nested text", "heading"));
        ExtractionDocumentTableElement table = new(new TextContent("table local"), 2, 2);
        ExtractionDocumentSection cellSection = new();
        cellSection.Elements.Add(Text("cell paragraph"));
        cellSection.Elements.Add(new ExtractionDocumentImageElement(text: "cell image text"));
        table.Cells[0, 0] = new(Text("merged header"), columnSpan: 2);
        table.Cells[1, 0] = new(cellSection);
        table.Cells[1, 1] = new();
        ExtractionDocumentImageElement image = new(new TextContent("image local"), "image text")
        {
            AlternativeText = "alternative text",
        };
        ExtractionDocumentImageElement bytes = new(new DataContent(new byte[] { 1, 2, 3 }, "image/png"))
        {
            AlternativeText = "bytes alternative text",
        };
        page.Elements.AddRange([nested, table, image, bytes, Text("page footer", "pageFooter"), new CustomElement(new TextContent("custom"))]);
        document.Sections.Add(page);

        Assert.Equal(
            Join("document local", "section local", "nested text", "table local", "merged header", "cell paragraph",
                "cell image text", "image local", "image text", "page footer", "custom"),
            document.GetText());
        Assert.Equal(Join("table local", "merged header", "cell paragraph", "cell image text"), table.GetText());
        Assert.Equal(Join("cell paragraph", "cell image text"), table.Cells[1, 0]!.GetText());
        Assert.Equal(Join("image local", "image text"), image.GetText());
        Assert.Equal(string.Empty, bytes.GetText());
        Assert.Equal(string.Empty, table.Cells[1, 1]!.GetText());
    }

    [Fact]
    public void GetText_SkipsEmptyBlocksAndReturnsEmptyForStructureWithoutText()
    {
        ExtractionDocument document = new("empty");
        Assert.Equal(string.Empty, document.GetText());

        ExtractionDocumentSection section = new(new TextContent(string.Empty));
        section.Elements.Add(Text(string.Empty));
        section.Elements.Add(new ExtractionDocumentImageElement(text: string.Empty));
        section.Elements.Add(new ExtractionDocumentTableElement(null, 0, 0));
        section.Elements.Add(Text(" "));
        section.Elements.Add(Text("value"));
        document.Sections.Add(section);

        Assert.Equal(Join(" ", "value"), document.GetText());
    }

    [Fact]
    public void GetText_RepeatsSharedContentAndRejectsCyclesAndNulls()
    {
        ExtractionDocumentTextElement shared = Text("shared");
        ExtractionDocumentSection section = new();
        section.Elements.AddRange([shared, shared]);
        ExtractionDocumentTableElement inner = new(null, 1, 1);
        inner.Cells[0, 0] = new(shared);
        ExtractionDocumentTableElement outer = new(null, 1, 2);
        outer.Cells[0, 0] = new(inner);
        outer.Cells[0, 1] = new(inner);
        section.Elements.Add(outer);
        ExtractionDocument document = new("shared");
        document.Sections.AddRange([section, section]);

        Assert.Equal(Join(Enumerable.Repeat("shared", 8).ToArray()), document.GetText());

        inner.Cells[0, 0] = new(outer);
        Assert.Throws<InvalidOperationException>(() => document.GetText());
        Assert.Throws<InvalidOperationException>(() => outer.GetText());
        Assert.Throws<InvalidOperationException>(() => outer.Cells[0, 0]!.GetText());

        inner.Cells[0, 0] = null;
        section.Elements.Add(section);
        Assert.Throws<InvalidOperationException>(() => document.GetText());
        Assert.Throws<InvalidOperationException>(() => section.GetText());

        section.Elements[section.Elements.Count - 1] = null!;
        Assert.Throws<InvalidOperationException>(() => document.GetText());

        document.Sections[1] = null!;
        section.Elements.RemoveAt(section.Elements.Count - 1);
        Assert.Throws<InvalidOperationException>(() => document.GetText());
    }

    [Fact]
    public void ProviderShapes_ProduceTheSameTextRegardlessOfSectionAndCellShape()
    {
        ExtractionDocument[] shapes = [FlatPageShape(), NestedHeadingShape(), SectionValuedCellShape()];

        foreach (ExtractionDocument document in shapes)
        {
            Assert.Equal(Join(_expectedBlocks), document.GetText());
            Assert.Equal(
                Join(_expectedBlocks),
                Join(document.EnumerateContent().Select(element => element.GetText()).ToArray()));
        }

        // The two-level loop from the issue sees different text for each shape and never sees cells or image text.
        Assert.Equal(new[] { "Title", "Intro" }, TwoLevelTextLoop(shapes[0]));
        Assert.Equal(new[] { "Title" }, TwoLevelTextLoop(shapes[1]));
        Assert.Equal(new[] { "Title", "Intro" }, TwoLevelTextLoop(shapes[2]));
    }

    private static ExtractionDocument FlatPageShape()
    {
        ExtractionDocumentSection page = new();
        page.Elements.AddRange([Text("Title", "heading"), Text("Intro"), Table(Text("Name"), Text("Value"), Text("Alpha"), Text("1")), Figure()]);
        ExtractionDocument document = new("flat");
        document.Sections.Add(page);
        return document;
    }

    private static ExtractionDocument NestedHeadingShape()
    {
        ExtractionDocumentSection body = new();
        body.Elements.AddRange([Text("Intro"), Table(Text("Name"), Text("Value"), Text("Alpha"), Text("1"))]);
        ExtractionDocumentSection figures = new();
        figures.Elements.Add(Figure());
        ExtractionDocumentSection page = new();
        page.Elements.AddRange([Text("Title", "heading"), body, figures]);
        ExtractionDocument document = new("nested");
        document.Sections.Add(page);
        return document;
    }

    private static ExtractionDocument SectionValuedCellShape()
    {
        ExtractionDocumentSection page = new();
        page.Elements.AddRange([Text("Title", "heading"), Text("Intro"), Table(Wrap(Text("Name")), Text("Value"), Wrap(Text("Alpha")), Text("1"))]);
        ExtractionDocumentSection secondPage = new();
        secondPage.Elements.Add(Figure());
        ExtractionDocument document = new("cells");
        document.Sections.AddRange([page, secondPage]);
        return document;

        static ExtractionDocumentSection Wrap(ExtractionDocumentElement element)
        {
            ExtractionDocumentSection section = new();
            section.Elements.Add(element);
            return section;
        }
    }

    private static List<string> TwoLevelTextLoop(ExtractionDocument document)
    {
        List<string> text = [];
        foreach (ExtractionDocumentSection section in document.Sections)
        {
            foreach (ExtractionDocumentElement element in section.Elements)
            {
                if (element is ExtractionDocumentTextElement { Content: TextContent content })
                {
                    text.Add(content.Text);
                }
            }
        }

        return text;
    }

    private static ExtractionDocumentTableElement Table(params ExtractionDocumentElement[] cells)
    {
        ExtractionDocumentTableElement table = new(null, 2, 2);
        for (int i = 0; i < cells.Length; i++)
        {
            table.Cells[i / 2, i % 2] = new(cells[i], role: i < 2 ? "columnHeader" : null);
        }

        return table;
    }

    private static ExtractionDocumentImageElement Figure()
        => new(new UriContent("urn:figure", "image/png"), "Chart label") { AlternativeText = "A chart" };

    private static ExtractionDocumentTextElement Text(string value, string? role = null) => new(new TextContent(value), role);

    private static string Join(params string[] blocks) => string.Join(Environment.NewLine, blocks);

    private static IEnumerable<(int row, int column)> Positions(ExtractionDocumentTableElement table)
    {
        for (int row = 0; row < table.Cells.RowCount; row++)
        {
            for (int column = 0; column < table.Cells.ColumnCount; column++)
            {
                yield return (row, column);
            }
        }
    }

    private sealed class CustomElement(AIContent? content) : ExtractionDocumentElement(content);
}

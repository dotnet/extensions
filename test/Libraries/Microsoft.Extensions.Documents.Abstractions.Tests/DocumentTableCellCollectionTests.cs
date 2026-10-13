// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTableCellCollectionTests
{
    [Fact]
    public void EnumeratesNullableCellsInRowMajorOrder()
    {
        DocumentTableElement table = new("stored table", 2, 3);
        DocumentTextElement firstContent = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement lastContent = new(DocumentTextKind.Paragraph, "last");

        table.Cells.SetContent(0, 1, firstContent);
        table.Cells.SetContent(1, 2, lastContent);

        DocumentTableCell?[] cells = table.Cells.ToArray();

        Assert.Equal(6, cells.Length);
        Assert.Null(cells[0]);
        Assert.Same(firstContent, cells[1]!.Content);
        Assert.Null(cells[2]);
        Assert.Null(cells[3]);
        Assert.Null(cells[4]);
        Assert.Same(lastContent, cells[5]!.Content);
        Assert.Equal(
            new DocumentTableCell?[] { null, cells[1], null, null, null, cells[5] },
            cells);
    }

    [Fact]
    public void SetContent_AttachesNonSectionElement()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells.SetContent(0, 0, content);

        DocumentTableCell cell = table.Cells[0, 0]!;
        Assert.Same(content, cell.Content);
        Assert.Same(cell, table.Cells[0, 0]);
        Assert.Null(content.ParentSection);
        Assert.Equal("s0/e0/c0,0", content.Address);
    }

    [Fact]
    public void SupportsImageCellContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        ReadOnlyMemory<byte> imageContent = new byte[] { 1, 2, 3 };
        Uri source = new("urn:table-image:opaque-1");
        DocumentImageElement image = new("stored image")
        {
            Content = imageContent,
            MediaType = "image/png",
            Source = source,
            AlternativeText = "diagram"
        };

        table.Cells.SetContent(0, 0, image);

        DocumentImageElement actual = Assert.IsType<DocumentImageElement>(table.Cells[0, 0]!.Content);
        Assert.Same(image, actual);
        Assert.Equal(imageContent.ToArray(), actual.Content!.Value.ToArray());
        Assert.Equal("image/png", actual.MediaType);
        Assert.Same(source, actual.Source);
        Assert.Equal("diagram", actual.AlternativeText);
    }

    [Fact]
    public void ReplacesAndRemovesContent_DetachCellsAndElements()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement oldContent = new(DocumentTextKind.Paragraph, "old");
        DocumentTextElement replacementContent = new(DocumentTextKind.Paragraph, "replacement");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells.SetContent(0, 0, oldContent);
        DocumentTableCell oldCell = table.Cells[0, 0]!;

        table.Cells.SetContent(0, 0, replacementContent);

        DocumentTableCell replacementCell = table.Cells[0, 0]!;
        Assert.NotSame(oldCell, replacementCell);
        Assert.Same(replacementContent, replacementCell.Content);
        Assert.Null(oldCell.Address);
        Assert.Null(oldContent.Address);
        Assert.Equal("s0/e0/c0,0", replacementCell.Address);
        Assert.Equal("s0/e0/c0,0", replacementContent.Address);

        table.Cells.SetContent(0, 0, null);

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(replacementCell.Address);
        Assert.Null(replacementContent.Address);
    }

    [Fact]
    public void RejectsAlreadyOwnedCell()
    {
        DocumentTableElement firstTable = new("first", 1, 1);
        DocumentTableElement secondTable = new("second", 1, 1);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");
        DocumentTableCell cell = new(content);
        firstTable.Cells[0, 0] = cell;

        Assert.Throws<InvalidOperationException>(() => secondTable.Cells[0, 0] = cell);

        Assert.Same(cell, firstTable.Cells[0, 0]);
        Assert.Null(secondTable.Cells[0, 0]);
        Assert.Same(content, firstTable.Cells[0, 0]!.Content);
        Assert.Equal(0, cell.RowIndex);
        Assert.Equal(0, cell.ColumnIndex);
    }

    [Fact]
    public void RejectsSectionContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentSection section = new("section");

        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 0, section));

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(section.ParentSection);
        Assert.Null(section.Address);
    }

    [Fact]
    public void RejectsTableSelfContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);

        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 0, table));

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(table.Address);
    }

    [Fact]
    public void RejectsContentThatCreatesRelationshipCycle()
    {
        DocumentTableElement outerTable = new("outer", 1, 1);
        DocumentTableElement innerTable = new("inner", 1, 1);

        outerTable.Cells.SetContent(0, 0, innerTable);

        Assert.Throws<InvalidOperationException>(() => innerTable.Cells.SetContent(0, 0, outerTable));

        Assert.Same(innerTable, outerTable.Cells[0, 0]!.Content);
        Assert.Null(innerTable.Cells[0, 0]);
        Assert.Null(outerTable.Address);
        Assert.Null(innerTable.Address);
    }

    [Fact]
    public void RejectsContentAlreadyAttachedElsewhere()
    {
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");
        section.Elements.Add(content);

        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 0, content));

        Assert.Same(section, content.ParentSection);
        Assert.Same(content, section.Elements[0]);
        Assert.Null(table.Cells[0, 0]);
    }

    [Fact]
    public void RejectsDanglingTableRelationships()
    {
        DocumentTableElement table = new("stored table", 1, 2);
        DocumentTextElement firstContent = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement secondContent = new(DocumentTextKind.Paragraph, "second");
        DocumentSection invalidReplacement = new("invalid replacement");

        table.Cells.SetContent(0, 0, firstContent);
        table.Cells.SetContent(0, 1, secondContent);

        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 0, invalidReplacement));
        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 0, secondContent));

        Assert.Same(firstContent, table.Cells[0, 0]!.Content);
        Assert.Same(secondContent, table.Cells[0, 1]!.Content);
        Assert.Null(invalidReplacement.ParentSection);
        Assert.Null(invalidReplacement.Address);
        Assert.Null(firstContent.Address);
        Assert.Null(secondContent.Address);
    }

    [Fact]
    public void Mutations_IncrementDocumentVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement firstContent = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement replacementContent = new(DocumentTextKind.Paragraph, "replacement");
        document.Sections.Add(section);
        section.Elements.Add(table);

        long version = document.Version;
        table.Cells.SetContent(0, 0, firstContent);
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        table.Cells.SetContent(0, 0, replacementContent);
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        table.Cells.SetContent(0, 0, null);
        Assert.Equal(version + 1, document.Version);
    }

    [Fact]
    public void SameContentAssignment_DoesNotIncrementVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "content");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells.SetContent(0, 0, content);
        DocumentTableCell cell = table.Cells[0, 0]!;
        long version = document.Version;

        table.Cells[0, 0] = cell;

        Assert.Equal(version, document.Version);

        table.Cells.SetContent(0, 0, null);
        version = document.Version;
        table.Cells.SetContent(0, 0, null);

        Assert.Equal(version, document.Version);
    }
}

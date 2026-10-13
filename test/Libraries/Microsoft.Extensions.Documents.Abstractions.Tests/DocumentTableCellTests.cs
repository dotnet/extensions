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
}

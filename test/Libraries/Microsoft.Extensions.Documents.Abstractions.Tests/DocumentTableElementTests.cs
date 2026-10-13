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
    public void StoresDimensionsAndCreatesNullableCells()
    {
        DocumentTableElement table = new("stored table", 2, 3);

        Assert.Equal("stored table", table.StoredMarkdown);
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
    public void ZeroDimensions_ToMarkdownReturnsStoredMarkdown(int rowCount, int columnCount)
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

        Assert.Throws<ArgumentOutOfRangeException>(
            parameterName,
            () => _ = table.Cells[row, column]);

        Assert.Equal(
            new DocumentTableCell?[] { null, null, null, null },
            table.Cells.ToArray());
    }

    [Fact]
    public void ToMarkdown_ProjectsEveryCellAndUsesEmptyValueForNull()
    {
        DocumentTableElement table = new("stored table", 2, 3);
        table.Cells.SetContent(0, 1, new DocumentTextElement(DocumentTextKind.Paragraph, "one"));
        table.Cells.SetContent(1, 0, new DocumentTextElement(DocumentTextKind.Paragraph, "two"));

        Assert.Equal(
            $"|  | one |  |{Environment.NewLine}| two |  |  |",
            table.ToMarkdown());
    }

    [Fact]
    public void ToMarkdown_EscapesPipesInCellProjections()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        table.Cells.SetContent(
            0,
            0,
            new DocumentTextElement(DocumentTextKind.Paragraph, "left|right"));

        Assert.Equal("| left\\|right |", table.ToMarkdown());
    }

    [Fact]
    public void GetMarkdown_RemainsStoredMarkdown()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        table.Cells.SetContent(0, 0, new DocumentTextElement(DocumentTextKind.Paragraph, "semantic cell"));

        Assert.Equal("stored table", table.GetMarkdown());
        Assert.Equal("| semantic cell |", table.ToMarkdown());
    }
}

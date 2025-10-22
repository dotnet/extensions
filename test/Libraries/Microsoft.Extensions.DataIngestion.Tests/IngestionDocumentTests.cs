// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public class IngestionDocumentTests
{
    private readonly IngestionDocumentElement?[,] rows =
    {
        { new IngestionDocumentParagraph("header") },
        { new IngestionDocumentParagraph("row1") },
        { new IngestionDocumentParagraph("row2") }
    };

    [Fact]
    public void EnumeratorFlattensTheStructureAndPreservesOrder()
    {
        IngestionDocument doc = new("withSubSections");
        doc.Sections.Add(new IngestionDocumentSection("first section")
        {
            Elements =
            {
                new IngestionDocumentHeader("header"),
                new IngestionDocumentParagraph("paragraph"),
                new IngestionDocumentTable("table", rows),
                new IngestionDocumentSection("nested section")
                {
                    Elements =
                    {
                        new IngestionDocumentHeader("nested header"),
                        new IngestionDocumentParagraph("nested paragraph")
                    }
                }
            }
        });
        doc.Sections.Add(new IngestionDocumentSection("second section")
        {
            Elements =
            {
                new IngestionDocumentHeader("header 2"),
                new IngestionDocumentParagraph("paragraph 2")
            }
        });

        IngestionDocumentElement[] flatElements = doc.EnumerateContent().ToArray();

        Assert.IsType<IngestionDocumentHeader>(flatElements[0]);
        Assert.Equal("header", flatElements[0].GetMarkdown());
        Assert.IsType<IngestionDocumentParagraph>(flatElements[1]);
        Assert.Equal("paragraph", flatElements[1].GetMarkdown());
        Assert.IsType<IngestionDocumentTable>(flatElements[2]);
        Assert.Equal("table", flatElements[2].GetMarkdown());
        Assert.IsType<IngestionDocumentHeader>(flatElements[3]);
        Assert.Equal("nested header", flatElements[3].GetMarkdown());
        Assert.IsType<IngestionDocumentParagraph>(flatElements[4]);
        Assert.Equal("nested paragraph", flatElements[4].GetMarkdown());
        Assert.IsType<IngestionDocumentHeader>(flatElements[5]);
        Assert.Equal("header 2", flatElements[5].GetMarkdown());
        Assert.IsType<IngestionDocumentParagraph>(flatElements[6]);
        Assert.Equal("paragraph 2", flatElements[6].GetMarkdown());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyParagraphDocumentCantBeCreated(string? input)
        => Assert.Throws<ArgumentNullException>(() => new IngestionDocumentParagraph(input!));
}

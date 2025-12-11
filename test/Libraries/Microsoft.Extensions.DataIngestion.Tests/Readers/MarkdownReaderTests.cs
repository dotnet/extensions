// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

public class MarkdownReaderTests : DocumentReaderConformanceTests
{
    protected override IngestionDocumentReader CreateDocumentReader(bool extractImages = false) => new MarkdownReader();

    public static new TheoryData<string> Links =>
    [
        "https://raw.githubusercontent.com/microsoft/markitdown/main/README.md"
    ];

    [ConditionalTheory]
    [MemberData(nameof(Links))]
    public override Task SupportsStreams(string source) => base.SupportsStreams(source);

    [ConditionalTheory]
    [MemberData(nameof(Links))]
    public override Task SupportsFiles(string source) => base.SupportsFiles(source);

    [ConditionalFact]
    public override async Task SupportsTables()
    {
        string markdownContent = """
        # Key Milestones

        | **Milestone** | **Target Date** | **Department** | **Indicator** |
        | --- | --- | --- | --- |
        | Environmental Audit | Mar 2025 | Environmental | Audit Complete |
        | Renewable Energy Launch | Jul 2025 | Facilities | Install Operational |
        | Staff Workshop | Sep 2025 | HR | Workshop Held |
        | Emissions Review | Dec 2029 | All | 25% Emissions Cut |
        """;

        IngestionDocument document = await ReadAsync(markdownContent);

        IngestionDocumentTable documentTable = Assert.Single(document.EnumerateContent().OfType<IngestionDocumentTable>());
        Assert.Equal(5, documentTable.Cells.GetLength(0));
        Assert.Equal(4, documentTable.Cells.GetLength(1));

        string[,] expected =
        {
            { "**Milestone**", "**Target Date**", "**Department**", "**Indicator**" },
            { "Environmental Audit", "Mar 2025", "Environmental", "Audit Complete" },
            { "Renewable Energy Launch", "Jul 2025", "Facilities", "Install Operational" },
            { "Staff Workshop", "Sep 2025", "HR", "Workshop Held" },
            { "Emissions Review", "Dec 2029", "All", "25% Emissions Cut" }
        };

        Assert.Equal(expected, documentTable.Cells.Map(element => element!.GetMarkdown().Trim()));
    }

    [ConditionalFact]
    public override async Task SupportsImages()
    {
        string contentType1 = "image/png";
        byte[] imageBytes1 = Enumerable.Range(0, 55).Select(i => (byte)i).ToArray();
        string contentType2 = "image/jpeg";
        byte[] imageBytes2 = Enumerable.Range(55, 111).Select(i => (byte)i).ToArray();
        string contentType3 = "image/newfancy";
        byte[] imageBytes3 = Enumerable.Range(166, 200).Select(i => (byte)i).ToArray();

        string markdownContent = $"""
        # All content types supported!

        PNG is fine!

        ![One](data:{contentType1};base64,{Convert.ToBase64String(imageBytes1)})

        JPEG is also fine!

        ![Two](data:{contentType2};base64,{Convert.ToBase64String(imageBytes2)})

        But what about a new fancy type?

        ![Three](data:{contentType3};base64,{Convert.ToBase64String(imageBytes3)})
        """;

        IngestionDocument document = await ReadAsync(markdownContent);

        Assert.NotNull(document);
        var images = document.EnumerateContent().OfType<IngestionDocumentImage>().ToArray();
        Assert.Equal(3, images.Length);
        Assert.Equal(contentType1, images[0].MediaType);
        Assert.Equal(imageBytes1, images[0].Content?.ToArray());
        Assert.Equal("One", images[0].AlternativeText);
        Assert.Equal(contentType2, images[1].MediaType);
        Assert.Equal(imageBytes2, images[1].Content?.ToArray());
        Assert.Equal("Two", images[1].AlternativeText);
        Assert.Equal(contentType3, images[2].MediaType);
        Assert.Equal(imageBytes3, images[2].Content?.ToArray());
        Assert.Equal("Three", images[2].AlternativeText);
    }

    [ConditionalFact]
    public async Task SupportsTablesWithImages()
    {
        byte[] imageBytes = Enumerable.Range(55, 111).Select(i => (byte)i).ToArray();
        string markdownContent = $"""
        # Table with Images

        | **Years** | **Logo** |
        | --- | --- |
        | 2020-2025 | ![Latest logo](data:image/png;base64,{Convert.ToBase64String(imageBytes)}) |
        """;

        IngestionDocument document = await ReadAsync(markdownContent);

        var table = Assert.Single(document.EnumerateContent().OfType<IngestionDocumentTable>());
        Assert.Equal(2, table.Cells.GetLength(0));
        Assert.Equal(2, table.Cells.GetLength(1));

        // Each reader properly recognizes the text from the first column.
        // When it comes to the images, MarkItDown extracts them as images, while
        // other readers return nothing or ORCed text.
        Assert.Equal("**Years**", table.Cells[0, 0]!.GetMarkdown().Trim());
        Assert.Equal("**Logo**", table.Cells[0, 1]!.GetMarkdown().Trim());
        Assert.Equal("2020-2025", table.Cells[1, 0]!.GetMarkdown().Trim());

        IngestionDocumentImage img = Assert.IsType<IngestionDocumentImage>(table.Cells[1, 1]);
        Assert.Equal("image/png", img.MediaType);
        Assert.NotNull(img.Content);
        Assert.False(img.Content.Value.IsEmpty);
        Assert.Equal("Latest logo", img.AlternativeText);
    }

    [ConditionalFact]
    public async Task SupportsInlineHtml()
    {
        string markdownContent = """
        When getting the managed exception object for this exception, the runtime will first try to allocate a new managed object <sup>[1]</sup>,
        and if that fails, will return a pre-allocated, shared, global out of memory exception object.
        """;

        IngestionDocument document = await ReadAsync(markdownContent);

        Assert.NotNull(document);
        var paragraph = Assert.Single(document.EnumerateContent().OfType<IngestionDocumentParagraph>());
        Assert.NotNull(paragraph.Text);
        Assert.Contains("allocate a new managed object", paragraph.Text);
        Assert.Contains("<sup>[1]</sup>", paragraph.Text);
        Assert.Contains("out of memory exception object", paragraph.Text);
    }

    private async Task<IngestionDocument> ReadAsync(string content)
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(content));
        return await CreateDocumentReader().ReadAsync(stream, "id", "text/markdown");
    }
}

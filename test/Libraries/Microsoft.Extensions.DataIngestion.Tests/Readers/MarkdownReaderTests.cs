// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

public class MarkdownReaderTests : DocumentReaderConformanceTests
{
    protected override IngestionDocumentReader CreateDocumentReader(bool extractImages = false) => new MarkdownReader();

    public static new IEnumerable<object[]> Links
    {
        get
        {
            yield return new object[] { "https://raw.githubusercontent.com/microsoft/markitdown/main/README.md" };
        }
    }

    public static new IEnumerable<object[]> Files
    {
        get
        {
            yield return new object[] { Path.Combine("TestFiles", "Sample.md") };
        }
    }

    [ConditionalFact]
    public override Task SupportsTables() => SupportsTablesCore(Path.Combine("TestFiles", "Sample.md"));

    [ConditionalTheory]
    [MemberData(nameof(Links))]
    public override Task SupportsStreams(string uri) => base.SupportsStreams(uri);

    [ConditionalTheory]
    [MemberData(nameof(Files))]
    public override Task SupportsFiles(string filePath) => base.SupportsFiles(filePath);

    [ConditionalFact]
    public override Task SupportsImages() => SupportsImagesCore(Path.Combine("TestFiles", "SampleWithImage.md"));

    public override async Task SupportsTablesWithImages()
    {
        var table = await SupportsTablesWithImagesCore(Path.Combine("TestFiles", "TableWithImage.md"));

        for (int rowIndex = 1; rowIndex < table.Cells.GetLength(0); rowIndex++)
        {
            IngestionDocumentImage img = Assert.IsType<IngestionDocumentImage>(table.Cells[rowIndex, 1]);

            Assert.Equal("image/png", img.MediaType);
            Assert.NotNull(img.Content);
            Assert.False(img.Content.Value.IsEmpty);
        }
    }

    [Fact]
    public async Task CanParseVariousContentTypes()
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

        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(markdownContent));

        IngestionDocument document = await CreateDocumentReader().ReadAsync(stream, "doc1", "text/markdown");

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
}

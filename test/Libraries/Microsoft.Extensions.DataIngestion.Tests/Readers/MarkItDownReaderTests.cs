// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

[MarkItDownCondition]
public class MarkItDownReaderTests : DocumentReaderConformanceTests
{
    protected override IngestionDocumentReader CreateDocumentReader(bool extractImages = false)
        => MarkItDownConditionAttribute.IsInstalled.Value
        ? new MarkItDownReader(extractImages: extractImages)
        : throw new SkipTestException("MarkItDown is not installed");

    protected override void SimpleAsserts(IngestionDocument document, string source, string expectedId)
    {
        Assert.NotNull(document);
        Assert.Equal(expectedId, document.Identifier);
        Assert.NotEmpty(document.Sections);

        var elements = document.EnumerateContent().ToArray();

        bool isPdf = source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            // MarkItDown does a bad job of recognizing Headers and Tables even for simple PDF files.
            Assert.Contains(elements, element => element is IngestionDocumentHeader);
            Assert.Contains(elements, element => element is IngestionDocumentTable);
        }

        Assert.Contains(elements, element => element is IngestionDocumentParagraph);
        Assert.All(elements, element => Assert.NotEmpty(element.GetMarkdown()));
    }

    // The original purpose of the MarkItDown library was to support text-only LLMs.
    // Source: https://github.com/microsoft/markitdown/issues/56#issuecomment-2546357264
    // It can extract images, but the support is limited to some formats like docx.
    [ConditionalFact]
    public override Task SupportsImages()
        => SupportsImagesCore(Path.Combine("TestFiles", "SampleWithImage.docx"));

    [ConditionalFact]
    public override async Task SupportsTablesWithImages()
    {
        var table = await SupportsTablesWithImagesCore(Path.Combine("TestFiles", "TableWithImage.docx"));

        for (int rowIndex = 1; rowIndex < table.Cells.GetLength(0); rowIndex++)
        {
            IngestionDocumentImage img = Assert.IsType<IngestionDocumentImage>(table.Cells[rowIndex, 1]);

            Assert.Equal("image/png", img.MediaType);
            Assert.NotNull(img.Content);
            Assert.False(img.Content.Value.IsEmpty);
        }
    }
}

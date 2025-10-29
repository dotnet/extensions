// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.ML.Tokenizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests;

public class HeaderChunkerTests
{
    [Fact]
    public async Task CanChunkNonTrivialDocument()
    {
        IngestionDocument doc = new("nonTrivial");
        doc.Sections.Add(new()
        {
            Elements =
            {
                new IngestionDocumentHeader("Header 1") { Level = 1 },
                    new IngestionDocumentHeader("Header 1_1") { Level = 2 },
                        new IngestionDocumentParagraph("Paragraph 1_1_1"),
                        new IngestionDocumentHeader("Header 1_1_1") { Level = 3 },
                            new IngestionDocumentParagraph("Paragraph 1_1_1_1"),
                            new IngestionDocumentParagraph("Paragraph 1_1_1_2"),
                        new IngestionDocumentHeader("Header 1_1_2") { Level = 3 },
                            new IngestionDocumentParagraph("Paragraph 1_1_2_1"),
                            new IngestionDocumentParagraph("Paragraph 1_1_2_2"),
                    new IngestionDocumentHeader("Header 1_2") { Level = 2 },
                        new IngestionDocumentParagraph("Paragraph 1_2_1"),
                        new IngestionDocumentHeader("Header 1_2_1") { Level = 3 },
                            new IngestionDocumentParagraph("Paragraph 1_2_1_1"),
            }
        });

        HeaderChunker chunker = new(new(TiktokenTokenizer.CreateForModel("gpt-4")));
        IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.Equal(5, chunks.Count);
        string nl = Environment.NewLine;
        Assert.Equal("Header 1 Header 1_1", chunks[0].Context);
        Assert.Equal($"Header 1 Header 1_1{nl}Paragraph 1_1_1", chunks[0].Content);
        Assert.Equal("Header 1 Header 1_1 Header 1_1_1", chunks[1].Context);
        Assert.Equal($"Header 1 Header 1_1 Header 1_1_1{nl}Paragraph 1_1_1_1{nl}Paragraph 1_1_1_2", chunks[1].Content);
        Assert.Equal("Header 1 Header 1_1 Header 1_1_2", chunks[2].Context);
        Assert.Equal($"Header 1 Header 1_1 Header 1_1_2{nl}Paragraph 1_1_2_1{nl}Paragraph 1_1_2_2", chunks[2].Content);
        Assert.Equal("Header 1 Header 1_2", chunks[3].Context);
        Assert.Equal($"Header 1 Header 1_2{nl}Paragraph 1_2_1", chunks[3].Content);
        Assert.Equal("Header 1 Header 1_2 Header 1_2_1", chunks[4].Context);
        Assert.Equal($"Header 1 Header 1_2 Header 1_2_1{nl}Paragraph 1_2_1_1", chunks[4].Content);
    }

    [Fact]
    public async Task CanRespectTokenLimit()
    {
        IngestionDocument doc = new("longOne");
        doc.Sections.Add(new()
        {
            Elements =
            {
                new IngestionDocumentHeader("Header A") { Level = 1 },
                    new IngestionDocumentHeader("Header B") { Level = 2 },
                        new IngestionDocumentHeader("Header C") { Level = 3 },
                            new IngestionDocumentParagraph("This is a very long text. It's expressed with plenty of tokens")
            }
        });

        HeaderChunker chunker = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 13 });
        IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Header A Header B Header C", chunks[0].Context);
        Assert.Equal($"Header A Header B Header C\nThis is a very long text.", chunks[0].Content, ignoreLineEndingDifferences: true);
        Assert.Equal("Header A Header B Header C", chunks[1].Context);
        Assert.Equal($"Header A Header B Header C\n It's expressed with plenty of tokens", chunks[1].Content, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ThrowsWhenLimitIsTooLowToFitAnythingMoreThanContext()
    {
        IngestionDocument doc = new("longOne");
        doc.Sections.Add(new()
        {
            Elements =
            {
                new IngestionDocumentHeader("Header A") { Level = 1 }, // 2 tokens
                    new IngestionDocumentHeader("Header B") { Level = 2 }, // 2 tokens
                        new IngestionDocumentHeader("Header C") { Level = 3 }, // 2 tokens
                            new IngestionDocumentParagraph("This is a very long text. It's expressed with plenty of tokens")
            }
        });

        HeaderChunker lessThanContext = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 5 });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await lessThanContext.ProcessAsync(doc).ToListAsync());

        HeaderChunker sameAsContext = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 6 });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sameAsContext.ProcessAsync(doc).ToListAsync());
    }

    [Fact]
    public async Task CanSplitLongerParagraphsOnNewLine()
    {
        IngestionDocument doc = new("withNewLines");
        doc.Sections.Add(new()
        {
            Elements =
            {
                new IngestionDocumentHeader("Header A") { Level = 1 },
                    new IngestionDocumentHeader("Header B") { Level = 2 },
                        new IngestionDocumentHeader("Header C") { Level = 3 },
                            new IngestionDocumentParagraph(
@"This is a very long text. It's expressed with plenty of tokens. And it contains a new line.
With some text after the new line."),
                            new IngestionDocumentParagraph("And following paragraph.")
            }
        });

        HeaderChunker chunker = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 30 });
        IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Header A Header B Header C", chunks[0].Context);
        Assert.Equal($"Header A Header B Header C\nThis is a very long text. It's expressed with plenty of tokens. And it contains a new line.\n",
            chunks[0].Content, ignoreLineEndingDifferences: true);
        Assert.Equal("Header A Header B Header C", chunks[1].Context);
        Assert.Equal($"Header A Header B Header C\nWith some text after the new line.\nAnd following paragraph.", chunks[1].Content, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ThrowsWhenHeaderSeparatorAndSingleRowExceedTokenLimit()
    {
        IngestionDocument document = CreateDocumentWithLargeTable();

        // It takes 38 tokens to represent Headers, Separator and the first Row.
        HeaderChunker chunker = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 37 });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await chunker.ProcessAsync(document).ToListAsync());
    }

    [Fact]
    public async Task CanSplitLargeTableIntoMultipleChunks_MultipleRowsPerChunk()
    {
        IngestionDocument document = CreateDocumentWithLargeTable();

        HeaderChunker chunker = new(new(TiktokenTokenizer.CreateForModel("gpt-4")) { MaxTokensPerChunk = 100 });
        IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(document).ToListAsync();

        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, chunk => Assert.Equal("Header A", chunk.Context));
        Assert.Equal(
@"Header A
This is some text that describes why we need the following table.
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 0 | 1 | 2 | 3 | 4 |
| 5 | 6 | 7 | 8 | 9 |
| 10 | 11 | 12 | 13 | 14 |", chunks[0].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 15 | 16 | 17 | 18 | 19 |
| 20 | 21 | 22 | 23 | 24 |
And some follow up.", chunks[1].Content, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task CanSplitLargeTableIntoMultipleChunks_OneRowPerChunk()
    {
        IngestionDocument document = CreateDocumentWithLargeTable();

        Tokenizer tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
        HeaderChunker chunker = new(new(tokenizer) { MaxTokensPerChunk = 50 });
        IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(document).ToListAsync();

        Assert.Equal(6, chunks.Count);
        Assert.All(chunks, chunk => Assert.Equal("Header A", chunk.Context));
        Assert.All(chunks, chunk => Assert.InRange(tokenizer.CountTokens(chunk.Content), 1, 50));

        Assert.Equal(
@"Header A
This is some text that describes why we need the following table.", chunks[0].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 0 | 1 | 2 | 3 | 4 |", chunks[1].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 5 | 6 | 7 | 8 | 9 |", chunks[2].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 10 | 11 | 12 | 13 | 14 |", chunks[3].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 15 | 16 | 17 | 18 | 19 |", chunks[4].Content, ignoreLineEndingDifferences: true);
        Assert.Equal(
@"Header A
| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 20 | 21 | 22 | 23 | 24 |
And some follow up.", chunks[5].Content, ignoreLineEndingDifferences: true);
    }

    private static IngestionDocument CreateDocumentWithLargeTable()
    {
        IngestionDocumentTable table = new(
@"| one | two | three | four | five |
| --- | --- | --- | --- | --- |
| 0 | 1 | 2 | 3 | 4 |
| 5 | 6 | 7 | 8 | 9 |
| 10 | 11 | 12 | 13 | 14 |
| 15 | 16 | 17 | 18 | 19 |
| 20 | 21 | 22 | 23 | 24 |",
    CreateTableCells()
);

        IngestionDocument doc = new("withNewLines");
        doc.Sections.Add(new()
        {
            Elements =
            {
                new IngestionDocumentHeader("Header A") { Level = 1 },
                    new IngestionDocumentParagraph("This is some text that describes why we need the following table."),
                    table,
                    new IngestionDocumentParagraph("And some follow up.")
            }
        });

        return doc;

        static IngestionDocumentElement?[,] CreateTableCells()
        {
            var cells = new IngestionDocumentElement[6, 5]; // 6 rows (1 header + 5 data rows), 5 columns

            // Header row
            cells[0, 0] = new IngestionDocumentParagraph("one");
            cells[0, 1] = new IngestionDocumentParagraph("two");
            cells[0, 2] = new IngestionDocumentParagraph("three");
            cells[0, 3] = new IngestionDocumentParagraph("four");
            cells[0, 4] = new IngestionDocumentParagraph("five");

            // Data rows (0-29)
            int number = 0;
            for (int row = 1; row <= 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    cells[row, col] = new IngestionDocumentParagraph(number.ToString());
                    number++;
                }
            }

            return cells;
        }
    }

    // We need plenty of more tests here, especially for edge cases:
    // - sentence splitting
    // - markdown splitting (e.g. lists, code blocks etc.)
}

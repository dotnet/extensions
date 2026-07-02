// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests;

public class ChunkerMetadataPropagationTests
{
    private static string GetText(IngestionChunk chunk) => (chunk.Content as TextContent)?.Text ?? string.Empty;

    private static IngestionChunker CreateSectionChunker(int maxTokensPerChunk = 2_000)
    {
        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        return new SectionChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = 0 });
    }

    private static IngestionChunker CreateHeaderChunker(int maxTokensPerChunk = 2_000)
    {
        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        return new HeaderChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = 0 });
    }

    private static IngestionChunker CreateDocumentTokenChunker(int maxTokensPerChunk = 2_000)
    {
        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        return new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = 0 });
    }

    [Fact]
    public async Task SectionChunker_SingleElementWithMetadata_PropagatesMetadata()
    {
        var paragraph = new IngestionDocumentParagraph("This is a paragraph.");
        paragraph.Metadata["element_type"] = "text";
        paragraph.Metadata["page"] = 1;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
        Assert.Equal(1, chunk.Metadata["page"]);
    }

    [Fact]
    public async Task SectionChunker_MultipleElementsDifferentKeys_AllKeysAppear()
    {
        var para1 = new IngestionDocumentParagraph("First paragraph.");
        para1.Metadata["element_type"] = "text";

        var para2 = new IngestionDocumentParagraph("Second paragraph.");
        para2.Metadata["confidence"] = 0.95;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
        Assert.Equal(0.95, chunk.Metadata["confidence"]);
    }

    [Fact]
    public async Task SectionChunker_ConflictingKeys_FirstElementWins()
    {
        var para1 = new IngestionDocumentParagraph("First paragraph.");
        para1.Metadata["element_type"] = "table";

        var para2 = new IngestionDocumentParagraph("Second paragraph.");
        para2.Metadata["element_type"] = "text";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.Equal("table", chunk.Metadata["element_type"]);
    }

    [Fact]
    public async Task SectionChunker_NullMetadataValue_Skipped()
    {
        var paragraph = new IngestionDocumentParagraph("This is a paragraph.");
        paragraph.Metadata["element_type"] = null;
        paragraph.Metadata["valid_key"] = "valid_value";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.False(chunk.Metadata.ContainsKey("element_type"));
        Assert.Equal("valid_value", chunk.Metadata["valid_key"]);
    }

    [Fact]
    public async Task SectionChunker_NoMetadata_ChunkHasNoMetadata()
    {
        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection
        {
            Elements =
            {
                new IngestionDocumentParagraph("No metadata here."),
                new IngestionDocumentParagraph("Also no metadata.")
            }
        });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.False(chunk.HasMetadata);
    }

    [Fact]
    public async Task SectionChunker_ElementSplitAcrossChunks_FirstChunkGetsMetadata()
    {
        // Create a large paragraph that exceeds the token limit and forces a split
        string longText = string.Join(" ", Enumerable.Repeat("word", 600));
        var paragraph = new IngestionDocumentParagraph(longText);
        paragraph.Metadata["element_type"] = "body";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var chunker = CreateSectionChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count > 1);

        // First chunk gets the metadata
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("body", chunks[0].Metadata["element_type"]);

        // Subsequent chunks from the same element do NOT get metadata (accumulator was cleared on commit)
        Assert.False(chunks[1].HasMetadata);
    }

    [Fact]
    public async Task SectionChunker_TwoSectionsWithMetadata_IndependentMetadataPerSection()
    {
        var para1 = new IngestionDocumentParagraph("First section paragraph.");
        para1.Metadata["section"] = "intro";

        var para2 = new IngestionDocumentParagraph("Second section paragraph.");
        para2.Metadata["section"] = "conclusion";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1 } });
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para2 } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.Equal(2, chunks.Count);
        Assert.Equal("intro", chunks[0].Metadata["section"]);
        Assert.Equal("conclusion", chunks[1].Metadata["section"]);
    }

    [Fact]
    public async Task HeaderChunker_PropagatesMetadata()
    {
        var header = new IngestionDocumentHeader("# Title") { Level = 1 };
        var para = new IngestionDocumentParagraph("Body text.");
        para.Metadata["element_type"] = "text";
        para.Metadata["page"] = 3;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { header, para } });

        var chunker = CreateHeaderChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
        Assert.Equal(3, chunk.Metadata["page"]);
    }

    [Fact]
    public async Task DocumentTokenChunker_SingleElementWithMetadata_PropagatesMetadata()
    {
        var paragraph = new IngestionDocumentParagraph("This is a paragraph.");
        paragraph.Metadata["element_type"] = "text";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var chunker = CreateDocumentTokenChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
    }

    [Fact]
    public async Task DocumentTokenChunker_MultipleElements_AccumulatesMetadata()
    {
        var para1 = new IngestionDocumentParagraph("First paragraph.");
        para1.Metadata["element_type"] = "text";

        var para2 = new IngestionDocumentParagraph("Second paragraph.");
        para2.Metadata["confidence"] = 0.9;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var chunker = CreateDocumentTokenChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
        Assert.Equal(0.9, chunk.Metadata["confidence"]);
    }

    [Fact]
    public async Task DocumentTokenChunker_ConflictingKeys_FirstElementWins()
    {
        var para1 = new IngestionDocumentParagraph("First paragraph.");
        para1.Metadata["element_type"] = "table";

        var para2 = new IngestionDocumentParagraph("Second paragraph.");
        para2.Metadata["element_type"] = "text";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var chunker = CreateDocumentTokenChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.Equal("table", chunk.Metadata["element_type"]);
    }

    [Fact]
    public async Task DocumentTokenChunker_ElementSplitAcrossChunks_FirstChunkGetsMetadata()
    {
        string longText = string.Join(" ", Enumerable.Repeat("word", 600));
        var paragraph = new IngestionDocumentParagraph(longText);
        paragraph.Metadata["element_type"] = "body";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var chunker = CreateDocumentTokenChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count > 1);

        // First chunk gets the metadata
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("body", chunks[0].Metadata["element_type"]);

        // Subsequent chunks from the same element do NOT get metadata (cleared on finalize)
        Assert.False(chunks[1].HasMetadata);
    }

    [Fact]
    public async Task DocumentTokenChunker_NoMetadata_ChunkHasNoMetadata()
    {
        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection
        {
            Elements =
            {
                new IngestionDocumentParagraph("No metadata here.")
            }
        });

        var chunker = CreateDocumentTokenChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.False(chunk.HasMetadata);
    }

    [Fact]
    public async Task SectionChunker_TableWithMetadata_PropagatesMetadata()
    {
        var cells = new IngestionDocumentElement?[2, 2]
        {
            { new IngestionDocumentParagraph("Header1"), new IngestionDocumentParagraph("Header2") },
            { new IngestionDocumentParagraph("Value1"), new IngestionDocumentParagraph("Value2") }
        };
        var table = new IngestionDocumentTable("| Header1 | Header2 |\n| --- | --- |\n| Value1 | Value2 |", cells);
        table.Metadata["element_type"] = "table";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { table } });

        var chunker = CreateSectionChunker();
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("table", chunk.Metadata["element_type"]);
    }

    [Fact]
    public async Task SectionChunker_PreviousElementFillsChunk_NextElementMetadataOnNewChunk()
    {
        // First element exceeds the chunk limit, so it fills chunk 0 and overflows into chunk 1.
        // Second element is small and goes into the last chunk.
        // Each element has a unique metadata key — verify they end up on the correct chunks.
        string fillerText = string.Join(" ", Enumerable.Repeat("word", 600));
        var filler = new IngestionDocumentParagraph(fillerText);
        filler.Metadata["filler_key"] = "from_filler";

        var nextElement = new IngestionDocumentParagraph("Next element content here.");
        nextElement.Metadata["next_key"] = "from_next";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { filler, nextElement } });

        var chunker = CreateSectionChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count >= 2);

        // First chunk must have filler metadata (it contributed content to this chunk)
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("from_filler", chunks[0].Metadata["filler_key"]);
        Assert.False(chunks[0].Metadata.ContainsKey("next_key"));

        // The last chunk must have the next element's metadata
        var lastChunk = chunks[chunks.Count - 1];
        Assert.True(lastChunk.HasMetadata);
        Assert.Equal("from_next", lastChunk.Metadata["next_key"]);
    }

    [Fact]
    public async Task SectionChunker_NonTableElementTooLargeForCurrentChunk_MetadataOnCorrectChunks()
    {
        // Two large elements with the same metadata key but different values.
        // Each element exceeds chunk limit. Verify first-wins semantics per chunk:
        // - Chunks containing elem1 content get elem1's metadata (only the first such chunk)
        // - Chunks containing elem2 content get elem2's metadata (only the first such chunk)
        var elem1 = new IngestionDocumentParagraph(string.Join(" ", Enumerable.Repeat("alpha", 300)));
        elem1.Metadata["source"] = "elem1";

        var elem2 = new IngestionDocumentParagraph(string.Join(" ", Enumerable.Repeat("beta", 300)));
        elem2.Metadata["source"] = "elem2";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { elem1, elem2 } });

        var chunker = CreateSectionChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count >= 3);

        // First chunk: elem1's metadata (elem1 contributes content)
        Assert.Equal("elem1", chunks[0].Metadata["source"]);

        // Find the first chunk that contains elem2's content
        var firstElem2Chunk = chunks.First(c => GetText(c).Contains("beta"));
        Assert.True(firstElem2Chunk.HasMetadata);
        Assert.Equal("elem2", firstElem2Chunk.Metadata["source"]);
    }

    [Fact]
    public async Task SectionChunker_TablePreCommit_TableMetadataNotOnPreviousChunk()
    {
        // Previous content fills most of the chunk. Table header doesn't fit, forcing a pre-commit.
        // Table metadata must go on the chunk with the table, not the pre-committed chunk.
        // Use different metadata keys to distinguish elements.
        var filler = new IngestionDocumentParagraph(string.Join(" ", Enumerable.Repeat("fill", 500)));
        filler.Metadata["paragraph_key"] = "paragraph_value";

        var cells = new IngestionDocumentElement?[2, 2]
        {
            { new IngestionDocumentParagraph("Col1"), new IngestionDocumentParagraph("Col2") },
            { new IngestionDocumentParagraph("Val1"), new IngestionDocumentParagraph("Val2") }
        };
        var table = new IngestionDocumentTable("| Col1 | Col2 |\n| --- | --- |\n| Val1 | Val2 |", cells);
        table.Metadata["table_key"] = "table_value";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { filler, table } });

        var chunker = CreateSectionChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count >= 2);

        // Find the chunk containing table content
        var tableChunk = chunks.FirstOrDefault(c => GetText(c).Contains("Col1") || GetText(c).Contains("Val1"));
        Assert.NotNull(tableChunk);

        // The table chunk must have the table's metadata
        Assert.True(tableChunk!.HasMetadata);
        Assert.Equal("table_value", tableChunk.Metadata["table_key"]);

        // Chunks before the table chunk should NOT have table metadata
        int tableChunkIndex = chunks.IndexOf(tableChunk);
        for (int i = 0; i < tableChunkIndex; i++)
        {
            Assert.False(chunks[i].Metadata.ContainsKey("table_key"),
                $"Chunk {i} should not have table metadata");
        }
    }

    [Fact]
    public async Task DocumentTokenChunker_PreviousElementFillsChunk_NextElementMetadataOnNewChunk()
    {
        // First element exceeds chunk limit, second element is small.
        // Each has unique keys — verify correct chunk association.
        string fillerText = string.Join(" ", Enumerable.Repeat("word", 600));
        var filler = new IngestionDocumentParagraph(fillerText);
        filler.Metadata["filler_key"] = "from_filler";

        var nextElement = new IngestionDocumentParagraph("Next element with metadata.");
        nextElement.Metadata["next_key"] = "from_next";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { filler, nextElement } });

        var chunker = CreateDocumentTokenChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count >= 2);

        // First chunk must have filler metadata
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("from_filler", chunks[0].Metadata["filler_key"]);
        Assert.False(chunks[0].Metadata.ContainsKey("next_key"));

        // The last chunk must have the next element's metadata
        var lastChunk = chunks[chunks.Count - 1];
        Assert.True(lastChunk.HasMetadata);
        Assert.Equal("from_next", lastChunk.Metadata["next_key"]);
    }

    [Fact]
    public async Task DocumentTokenChunker_WithOverlap_PropagatesMetadata()
    {
        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        var chunker = new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = 200, OverlapTokens = 50 });

        string text1 = string.Join(" ", Enumerable.Repeat("alpha", 300));
        var para1 = new IngestionDocumentParagraph(text1);
        para1.Metadata["section"] = "intro";

        string text2 = string.Join(" ", Enumerable.Repeat("beta", 100));
        var para2 = new IngestionDocumentParagraph(text2);
        para2.Metadata["section"] = "body";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count >= 2);

        // First chunk should have intro metadata
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("intro", chunks[0].Metadata["section"]);
    }

    [Fact]
    public async Task SectionChunker_TableSplitAcrossChunks_FirstChunkGetsMetadata()
    {
        // A large table that spans multiple chunks — only the first chunk containing table content gets metadata
        int rowCount = 30;
        int colCount = 3;
        var cells = new IngestionDocumentElement?[rowCount, colCount];
        cells[0, 0] = new IngestionDocumentParagraph("HeaderColumn1");
        cells[0, 1] = new IngestionDocumentParagraph("HeaderColumn2");
        cells[0, 2] = new IngestionDocumentParagraph("HeaderColumn3");

        // Build a proper markdown string that's long enough to exceed the token limit
        var mdBuilder = new System.Text.StringBuilder();
        mdBuilder.AppendLine("| HeaderColumn1 | HeaderColumn2 | HeaderColumn3 |");
        mdBuilder.AppendLine("| --- | --- | --- |");

        for (int i = 1; i < rowCount; i++)
        {
            string c1 = $"Row{i} first column value with extra text to increase token count";
            string c2 = $"Row{i} second column value with extra text to increase token count";
            string c3 = $"Row{i} third column value with extra text to increase token count";
            cells[i, 0] = new IngestionDocumentParagraph(c1);
            cells[i, 1] = new IngestionDocumentParagraph(c2);
            cells[i, 2] = new IngestionDocumentParagraph(c3);
            mdBuilder.AppendLine($"| {c1} | {c2} | {c3} |");
        }

        var table = new IngestionDocumentTable(mdBuilder.ToString(), cells);
        table.Metadata["element_type"] = "data_table";

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { table } });

        var chunker = CreateSectionChunker(maxTokensPerChunk: 200);
        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        Assert.True(chunks.Count > 1, $"Table should span multiple chunks but got {chunks.Count}");

        // First chunk gets table metadata
        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("data_table", chunks[0].Metadata["element_type"]);

        // Subsequent table chunks do NOT get metadata (cleared on commit, first-wins)
        for (int i = 1; i < chunks.Count; i++)
        {
            Assert.False(chunks[i].HasMetadata, $"Chunk {i} should not have metadata");
        }
    }

    [Fact]
    public async Task SemanticSimilarityChunker_SingleElementWithMetadata_PropagatesMetadata()
    {
        var paragraph = new IngestionDocumentParagraph("This is a paragraph for semantic chunking.");
        paragraph.Metadata["element_type"] = "text";
        paragraph.Metadata["page"] = 1;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { paragraph } });

        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        using TestEmbeddingGenerator<TextContent, Embedding<float>> embeddingGenerator = new()
        {
            GenerateAsyncCallback = static async (values, options, ct) =>
            {
                var embeddings = values.Select(v =>
                    new Embedding<float>(new float[] { 1.0f, 2.0f, 3.0f, 4.0f }))
                    .ToArray();
                return [.. embeddings];
            }
        };
        var chunker = new SemanticSimilarityChunker(
            embeddingGenerator,
            new(tokenizer) { MaxTokensPerChunk = 2_000, OverlapTokens = 0 });

        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        var chunk = Assert.Single(chunks);
        Assert.True(chunk.HasMetadata);
        Assert.Equal("text", chunk.Metadata["element_type"]);
        Assert.Equal(1, chunk.Metadata["page"]);
    }

    [Fact]
    public async Task SemanticSimilarityChunker_MultipleElementsDifferentKeys_AllKeysAppear()
    {
        var para1 = new IngestionDocumentParagraph("First paragraph about .NET development.");
        para1.Metadata["element_type"] = "text";

        var para2 = new IngestionDocumentParagraph("Second paragraph about cloud computing.");
        para2.Metadata["confidence"] = 0.95;

        var doc = new IngestionDocument("doc");
        doc.Sections.Add(new IngestionDocumentSection { Elements = { para1, para2 } });

        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        using TestEmbeddingGenerator<TextContent, Embedding<float>> embeddingGenerator = new()
        {
            GenerateAsyncCallback = static async (values, options, ct) =>
            {
                var embeddings = values.Select(v =>
                    new Embedding<float>(new float[] { 1.0f, 2.0f, 3.0f, 4.0f }))
                    .ToArray();
                return [.. embeddings];
            }
        };
        var chunker = new SemanticSimilarityChunker(
            embeddingGenerator,
            new(tokenizer) { MaxTokensPerChunk = 2_000, OverlapTokens = 0 });

        var chunks = await chunker.ProcessAsync(doc).ToListAsync();

        // Semantic chunker may split elements into separate chunks based on similarity.
        // Verify each chunk carries its originating element's metadata.
        Assert.Equal(2, chunks.Count);

        Assert.True(chunks[0].HasMetadata);
        Assert.Equal("text", chunks[0].Metadata["element_type"]);

        Assert.True(chunks[1].HasMetadata);
        Assert.Equal(0.95, chunks[1].Metadata["confidence"]);
    }
}

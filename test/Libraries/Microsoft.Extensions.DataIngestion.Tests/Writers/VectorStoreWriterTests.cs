// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DataIngestion.Tests;
using Microsoft.Extensions.VectorData;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public abstract class VectorStoreWriterTests
{
    [Fact]
    public async Task CanWriteChunks()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var definition = IngestedChunkRecord<Guid, string>.CreateCollectionDefinition(TestEmbeddingGenerator<string>.DimensionCount);
        var collection = vectorStore.GetCollection<Guid, IngestedChunkRecord<Guid, string>>("chunks", definition);

        using VectorStoreWriter<Guid, string, IngestedChunkRecord<Guid, string>> writer = new(collection);

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk = TestChunkFactory.CreateChunk("some content", document);

        List<IngestionChunk<string>> chunks = [chunk];

        Assert.False(testEmbeddingGenerator.WasCalled);
        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        IngestedChunkRecord<Guid, string> record = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 1)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.NotEqual(Guid.Empty, record.Key);
        Assert.Equal(documentId, record.DocumentId);
        Assert.Equal(chunks[0].Content, record.Content);
        Assert.True(testEmbeddingGenerator.WasCalled);
    }

    [Fact]
    public async Task CanWriteChunksWithMetadata()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var collection = vectorStore.GetCollection<Guid, TestChunkRecordWithMetadata>("chunks-meta");
        using VectorStoreWriter<Guid, string, TestChunkRecordWithMetadata> writer = new(collection);

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk = TestChunkFactory.CreateChunk("some content", document);
        chunk.Metadata["Classification"] = "important";

        List<IngestionChunk<string>> chunks = [chunk];

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        TestChunkRecordWithMetadata record = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 1)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.Equal(documentId, record.DocumentId);
        Assert.Equal(chunks[0].Content, record.Content);
        Assert.Equal("important", record.Classification);
    }

    [Fact]
    public async Task DoesSupportIncrementalIngestion()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var definition = IngestedChunkRecord<Guid, string>.CreateCollectionDefinition(TestEmbeddingGenerator<string>.DimensionCount);
        var collection = vectorStore.GetCollection<Guid, IngestedChunkRecord<Guid, string>>("chunks-incr", definition);

        using VectorStoreWriter<Guid, string, IngestedChunkRecord<Guid, string>> writer = new(
            collection,
            options: new()
            {
                IncrementalIngestion = true,
            });

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk1 = TestChunkFactory.CreateChunk("first chunk", document);
        IngestionChunk<string> chunk2 = TestChunkFactory.CreateChunk("second chunk", document);

        List<IngestionChunk<string>> chunks = [chunk1, chunk2];

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 100)
            .CountAsync();
        Assert.Equal(chunks.Count, recordCount);

        // Now we will do an incremental ingestion that updates the chunk(s).
        IngestionChunk<string> updatedChunk = TestChunkFactory.CreateChunk("different content", document);

        List<IngestionChunk<string>> updatedChunks = [updatedChunk];

        await writer.WriteAsync(updatedChunks.ToAsyncEnumerable());

        // We ask for 100 records, but we expect only 1 as the previous 2 should have been deleted.
        IngestedChunkRecord<Guid, string> record = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 100)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.NotEqual(Guid.Empty, record.Key);
        Assert.Equal("different content", record.Content);
    }

    public static TheoryData<int?, int[]> BatchingTestCases => new()
    {
        // Low limit: BatchTokenCount=1, each chunk upserted separately
        { 1, [10, 10, 10] },

        // Default limit: chunks fit in single batch
        { null, [1000, 1000, 1000] },

        // Single chunk exceeds limit
        { 100, [200, 10] },

        // Multiple batches needed
        { 100, [60, 60, 60, 60] }
    };

    [Theory]
    [MemberData(nameof(BatchingTestCases))]
    public async Task BatchesChunks(int? batchTokenCount, int[] chunkTokenCounts)
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var options = new VectorStoreWriterOptions { IncrementalIngestion = false };
        if (batchTokenCount.HasValue)
        {
            options.BatchTokenCount = batchTokenCount.Value;
        }

        var definition = IngestedChunkRecord<Guid, string>.CreateCollectionDefinition(TestEmbeddingGenerator<string>.DimensionCount);
        var collection = vectorStore.GetCollection<Guid, IngestedChunkRecord<Guid, string>>("chunks-batch", definition);

        using VectorStoreWriter<Guid, string, IngestedChunkRecord<Guid, string>> writer = new(
            collection,
            options: options);

        IngestionDocument document = new(documentId);
        List<IngestionChunk<string>> chunks = [];
        for (int i = 0; i < chunkTokenCounts.Length; i++)
        {
            chunks.Add(new($"chunk {i + 1}", document, context: null, tokenCount: chunkTokenCounts[i]));
        }

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 100)
            .CountAsync();

        Assert.Equal(chunks.Count, recordCount);
    }

    [Fact]
    public async Task IncrementalIngestion_WithManyRecords_DeletesAllPreExistingChunks()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var definition = IngestedChunkRecord<Guid, string>.CreateCollectionDefinition(TestEmbeddingGenerator<string>.DimensionCount);
        var collection = vectorStore.GetCollection<Guid, IngestedChunkRecord<Guid, string>>("chunks-many", definition);

        using VectorStoreWriter<Guid, string, IngestedChunkRecord<Guid, string>> writer = new(
            collection,
            options: new()
            {
                IncrementalIngestion = true,
            });

        IngestionDocument document = new(documentId);

        // Create more chunks than the MaxTopCount (1000) to test pagination
        // We create 2500 chunks to ensure multiple batches
        List<IngestionChunk<string>> chunks = [];
        for (int i = 0; i < 2500; i++)
        {
            chunks.Add(TestChunkFactory.CreateChunk($"chunk {i}", document));
        }

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 10000)
            .CountAsync();
        Assert.Equal(chunks.Count, recordCount);

        // Now we will do an incremental ingestion that should delete all pre-existing chunks
        List<IngestionChunk<string>> updatedChunks =
        [
            TestChunkFactory.CreateChunk("updated chunk 1", document),
            TestChunkFactory.CreateChunk("updated chunk 2", document)
        ];

        await writer.WriteAsync(updatedChunks.ToAsyncEnumerable());

        // Verify that all old records were deleted and only the new ones remain
        List<IngestedChunkRecord<Guid, string>> records = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 10000)
            .ToListAsync();

        Assert.Equal(updatedChunks.Count, records.Count);
        Assert.Contains(records, r => (string)r.Content! == "updated chunk 1");
        Assert.Contains(records, r => (string)r.Content! == "updated chunk 2");
    }

    protected abstract VectorStore CreateVectorStore(TestEmbeddingGenerator<string> testEmbeddingGenerator);
}

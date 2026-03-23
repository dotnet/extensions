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
    public async Task CanWriteChunksWithCustomDefinition()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        // User creates their own definition without using CreateDefaultCollectionDefinition,
        // using custom storage names to prove they can map to a pre-existing collection schema.
        VectorStoreCollectionDefinition definition = new()
        {
            Properties =
            {
                new VectorStoreKeyProperty(nameof(IngestionChunkVectorRecord<>.Key), typeof(Guid)) { StorageName = "custom_key" },
                new VectorStoreVectorProperty(nameof(IngestionChunkVectorRecord<>.Embedding), typeof(string), TestEmbeddingGenerator<string>.DimensionCount)
                {
                    StorageName = "custom_embedding",
                },
                new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<>.Content), typeof(string)) { StorageName = "custom_content" },
                new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<>.Context), typeof(string)) { StorageName = "custom_context" },
                new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<>.DocumentId), typeof(string))
                {
                    StorageName = "custom_documentid",
                    IsIndexed = true,
                },
            },
        };

        var collection = vectorStore.GetCollection<Guid, IngestionChunkVectorRecord<string>>("chunks-custom", definition);

        using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(collection);

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk = TestChunkFactory.CreateChunk("custom schema content", document);

        List<IngestionChunk<string>> chunks = [chunk];

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        IngestionChunkVectorRecord<string> record = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 1)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.NotEqual(Guid.Empty, record.Key);
        Assert.Equal(documentId, record.DocumentId);
        Assert.Equal(chunks[0].Content, record.Content);
    }

    [Fact]
    public async Task CanWriteChunks()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);

        var collection = vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord<string>, string>(
            "chunks", TestEmbeddingGenerator<string>.DimensionCount);

        using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(collection);

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk = TestChunkFactory.CreateChunk("some content", document);

        List<IngestionChunk<string>> chunks = [chunk];

        Assert.False(testEmbeddingGenerator.WasCalled);
        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        IngestionChunkVectorRecord<string> record = await writer.VectorStoreCollection
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
        using TestVectorStoreWriterWithMetadata writer = new(collection);

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

        var collection = vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord<string>, string>(
            "chunks-incr", TestEmbeddingGenerator<string>.DimensionCount);

        using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(
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
        IngestionChunkVectorRecord<string> record = await writer.VectorStoreCollection
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

        var collection = vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord<string>, string>(
            "chunks-batch", TestEmbeddingGenerator<string>.DimensionCount);

        using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(
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

        var collection = vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord<string>, string>(
            "chunks-many", TestEmbeddingGenerator<string>.DimensionCount);

        using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(
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
        List<IngestionChunkVectorRecord<string>> records = await writer.VectorStoreCollection
            .GetAsync(filter: record => record.DocumentId == documentId, top: 10000)
            .ToListAsync();

        Assert.Equal(updatedChunks.Count, records.Count);
        Assert.Contains(records, r => r.Content == "updated chunk 1");
        Assert.Contains(records, r => r.Content == "updated chunk 2");
    }

    protected abstract VectorStore CreateVectorStore(TestEmbeddingGenerator<string> testEmbeddingGenerator);
}

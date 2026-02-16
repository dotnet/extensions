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
    public async Task CanGenerateDynamicSchema()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);
        using VectorStoreWriter<string> writer = new(
            vectorStore,
            dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk = TestChunkFactory.CreateChunk("some content", document);
        chunk.Metadata["key1"] = "value1";
        chunk.Metadata["key2"] = 123;
        chunk.Metadata["key3"] = true;
        chunk.Metadata["key4"] = 123.45;

        List<IngestionChunk<string>> chunks = [chunk];

        Assert.False(testEmbeddingGenerator.WasCalled);
        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        Dictionary<string, object?> record = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 1)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.NotNull(record["key"]);
        Assert.Equal(documentId, record["documentid"]);
        Assert.Equal(chunks[0].Content, record["content"]);
        Assert.True(testEmbeddingGenerator.WasCalled);
        foreach (var kvp in chunks[0].Metadata)
        {
            Assert.True(record.ContainsKey(kvp.Key), $"Record does not contain key '{kvp.Key}'");
            Assert.Equal(kvp.Value, record[kvp.Key]);
        }
    }

    [Fact]
    public async Task DoesSupportIncrementalIngestion()
    {
        string documentId = Guid.NewGuid().ToString();

        using TestEmbeddingGenerator<string> testEmbeddingGenerator = new();
        using VectorStore vectorStore = CreateVectorStore(testEmbeddingGenerator);
        using VectorStoreWriter<string> writer = new(
            vectorStore,
            dimensionCount: TestEmbeddingGenerator<string>.DimensionCount,
            options: new()
            {
                IncrementalIngestion = true,
            });

        IngestionDocument document = new(documentId);
        IngestionChunk<string> chunk1 = TestChunkFactory.CreateChunk("first chunk", document);
        chunk1.Metadata["key1"] = "value1";

        IngestionChunk<string> chunk2 = TestChunkFactory.CreateChunk("second chunk", document);

        List<IngestionChunk<string>> chunks = [chunk1, chunk2];

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 100)
            .CountAsync();
        Assert.Equal(chunks.Count, recordCount);

        // Now we will do an incremental ingestion that updates the chunk(s).
        IngestionChunk<string> updatedChunk = TestChunkFactory.CreateChunk("different content", document);
        updatedChunk.Metadata["key1"] = "value2";

        List<IngestionChunk<string>> updatedChunks = [updatedChunk];

        await writer.WriteAsync(updatedChunks.ToAsyncEnumerable());

        // We ask for 100 records, but we expect only 1 as the previous 2 should have been deleted.
        Dictionary<string, object?> record = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 100)
            .SingleAsync();

        Assert.NotNull(record);
        Assert.NotNull(record["key"]);
        Assert.Equal("different content", record["content"]);
        Assert.Equal("value2", record["key1"]);
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

        using VectorStoreWriter<string> writer = new(
            vectorStore,
            dimensionCount: TestEmbeddingGenerator<string>.DimensionCount,
            options: options);

        IngestionDocument document = new(documentId);
        List<IngestionChunk<string>> chunks = [];
        for (int i = 0; i < chunkTokenCounts.Length; i++)
        {
            chunks.Add(new($"chunk {i + 1}", document, context: null, tokenCount: chunkTokenCounts[i]));
        }

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 100)
            .CountAsync();

        Assert.Equal(chunks.Count, recordCount);
    }

    protected abstract VectorStore CreateVectorStore(TestEmbeddingGenerator<string> testEmbeddingGenerator);
}

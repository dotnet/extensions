// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        List<IngestionChunk<string>> chunks =
        [
            new("some content", document)
            {
                Metadata =
                {
                    { "key1", "value1" },
                    { "key2", 123 },
                    { "key3", true },
                    { "key4", 123.45 },
                }
            }
        ];

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
        List<IngestionChunk<string>> chunks =
        [
            new("first chunk", document)
            {
                Metadata =
                {
                    { "key1", "value1" }
                }
            },
            new("second chunk", document)
        ];

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 100)
            .CountAsync();
        Assert.Equal(chunks.Count, recordCount);

        // Now we will do an incremental ingestion that updates the chunk(s).
        List<IngestionChunk<string>> updatedChunks =
        [
            new("different content", document)
            {
                Metadata =
                {
                    { "key1", "value2" },
                }
            }
        ];

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

    [Fact]
    public async Task IncrementalIngestion_WithManyRecords_DeletesAllPreExistingChunks()
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

        // Create more chunks than the MaxTopCount in debug builds (10) to test pagination
        // In debug builds, MaxTopCount is 10, so we create 25 chunks to ensure multiple batches
        List<IngestionChunk<string>> chunks = [];
        for (int i = 0; i < 25; i++)
        {
            chunks.Add(new($"chunk {i}", document));
        }

        await writer.WriteAsync(chunks.ToAsyncEnumerable());

        int recordCount = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 1000)
            .CountAsync();
        Assert.Equal(chunks.Count, recordCount);

        // Now we will do an incremental ingestion that should delete all 25 pre-existing chunks
        List<IngestionChunk<string>> updatedChunks =
        [
            new("updated chunk 1", document),
            new("updated chunk 2", document)
        ];

        await writer.WriteAsync(updatedChunks.ToAsyncEnumerable());

        // Verify that all old records were deleted and only the new ones remain
        List<Dictionary<string, object?>> records = await writer.VectorStoreCollection
            .GetAsync(filter: record => (string)record["documentid"]! == documentId, top: 1000)
            .ToListAsync();

        Assert.Equal(updatedChunks.Count, records.Count);
        Assert.Contains(records, r => (string)r["content"]! == "updated chunk 1");
        Assert.Contains(records, r => (string)r["content"]! == "updated chunk 2");
    }

    protected abstract VectorStore CreateVectorStore(TestEmbeddingGenerator<string> testEmbeddingGenerator);
}

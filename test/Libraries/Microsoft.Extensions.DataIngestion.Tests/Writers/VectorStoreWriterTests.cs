// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

    protected abstract VectorStore CreateVectorStore(TestEmbeddingGenerator<string> testEmbeddingGenerator);
}

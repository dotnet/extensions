// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public class TestChunkRecordWithMetadata : IngestionChunkVectorRecord<string>
{
    public const int TestDimensionCount = 4;

    [VectorStoreVector(TestDimensionCount, StorageName = EmbeddingStorageName)]
    public override string? Embedding => Content;

    [VectorStoreData(StorageName = "classification")]
    public string? Classification { get; set; }
}

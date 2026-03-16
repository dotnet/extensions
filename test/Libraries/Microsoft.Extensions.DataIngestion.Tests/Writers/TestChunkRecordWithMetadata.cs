// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public class TestChunkRecordWithMetadata : IngestedChunkRecord<string>
{
    public const int TestDimensionCount = 4;

    [VectorStoreVector(TestDimensionCount, StorageName = EmbeddingPropertyName)]
    public override string? Embedding => Content;

    [VectorStoreData(StorageName = "classification")]
    [JsonPropertyName("classification")]
    public string? Classification { get; set; }

    public override void SetMetadata(string key, object? value)
    {
        switch (key)
        {
            case nameof(Classification):
                Classification = value as string;
                break;
        }
    }
}

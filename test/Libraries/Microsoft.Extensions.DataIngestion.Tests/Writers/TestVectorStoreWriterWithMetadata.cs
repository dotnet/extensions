// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public class TestVectorStoreWriterWithMetadata : VectorStoreWriter<string, TestChunkRecordWithMetadata>
{
    public TestVectorStoreWriterWithMetadata(VectorStoreCollection<Guid, TestChunkRecordWithMetadata> collection, VectorStoreWriterOptions? options = default)
        : base(collection, options)
    {
    }

    protected override void SetMetadata(TestChunkRecordWithMetadata record, string key, object? value)
    {
        switch (key)
        {
            case nameof(TestChunkRecordWithMetadata.Classification):
                record.Classification = value as string;
                break;
        }
    }
}

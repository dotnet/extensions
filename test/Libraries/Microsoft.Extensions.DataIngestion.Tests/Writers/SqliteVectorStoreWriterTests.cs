// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

#pragma warning disable MEAI001 // Tests use experimental mock embedding APIs

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public sealed class SqliteVectorStoreWriterTests : VectorStoreWriterTests, IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    public void Dispose() => File.Delete(_tempFile);

    protected override VectorStore CreateVectorStore(MockEmbeddingGenerator<string> testEmbeddingGenerator)
        => new SqliteVectorStore($"Data Source={_tempFile};Pooling=false", new() { EmbeddingGenerator = testEmbeddingGenerator });
}

#pragma warning restore MEAI001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public class IngestionChunkTests
{
    [Fact]
    public void Constructor_SetsTokenCountProperty()
    {
        var document = new IngestionDocument("test");
        var chunk = new IngestionChunk<string>("test content", document, 42);

        Assert.Equal(42, chunk.TokenCount);
    }

    [Fact]
    public void Constructor_ThrowsWhenTokenCountIsNegative()
    {
        var document = new IngestionDocument("test");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new IngestionChunk<string>("test content", document, -1));

        Assert.Equal("tokenCount", exception.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsZeroTokenCount()
    {
        var document = new IngestionDocument("test");
        var chunk = new IngestionChunk<string>("test content", document, 0);

        Assert.Equal(0, chunk.TokenCount);
    }
}

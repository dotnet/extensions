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
        IngestionChunk<string> chunk = new("test content", 42);

        Assert.Equal(42, chunk.TokenCount);
    }

    [Fact]
    public void Constructor_ThrowsWhenTokenCountIsNegative()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new IngestionChunk<string>("test content", -1));

        Assert.Equal("tokenCount", exception.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsWhenTokenCountIsZero()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new IngestionChunk<string>("test content", 0));

        Assert.Equal("tokenCount", exception.ParamName);
    }
}

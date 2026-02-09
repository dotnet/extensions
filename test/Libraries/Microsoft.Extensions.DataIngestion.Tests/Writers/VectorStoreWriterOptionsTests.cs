// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Writers.Tests;

public class VectorStoreWriterOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        VectorStoreWriterOptions options = new();

        Assert.Equal("chunks", options.CollectionName);
        Assert.True(options.IncrementalIngestion);
        Assert.Equal(512000, options.BatchTokenCount); // 256 * 2000
    }

    [Fact]
    public void BatchTokenCount_ShouldThrow_OnZeroOrNegative()
    {
        VectorStoreWriterOptions options = new();

        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.BatchTokenCount = 0);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.BatchTokenCount = -1);
    }

    [Fact]
    public void BatchTokenCount_CanBeSetToPositiveValue()
    {
        VectorStoreWriterOptions options = new()
        {
            BatchTokenCount = 100
        };

        Assert.Equal(100, options.BatchTokenCount);
    }
}

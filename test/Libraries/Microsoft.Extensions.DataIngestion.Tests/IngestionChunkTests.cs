// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public class IngestionChunkTests
{
    [Fact]
    public void Constructor_SetsTokenCountProperty()
    {
        IngestionDocument document = new("test");
        IngestionChunk<string> chunk = new("test content", document, 42);

        Assert.Equal(42, chunk.TokenCount);
    }

    [Fact]
    public void Constructor_ThrowsWhenTokenCountIsNegative()
    {
        IngestionDocument document = new("test");

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new IngestionChunk<string>("test content", document, -1));

        Assert.Equal("tokenCount", exception.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsWhenTokenCountIsZeroForNonEmptyContent()
    {
        IngestionDocument document = new("test");

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new IngestionChunk<string>("test content", document, 0));

        Assert.Equal("tokenCount", exception.ParamName);
    }
}

public static class TestHelpers
{
    private static readonly Tokenizer s_tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");

    public static IngestionChunk<string> CreateChunk(string content, IngestionDocument document)
    {
        int tokenCount = s_tokenizer.CountTokens(content, considerNormalization: false);
        return new IngestionChunk<string>(content, document, tokenCount);
    }
}

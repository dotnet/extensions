// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.ML.Tokenizers;

namespace Microsoft.Extensions.DataIngestion.Tests;

public static class TestChunkFactory
{
    private static readonly Tokenizer _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");

    public static IngestionChunk<string> CreateChunk(string content, IngestionDocument document)
    {
        int tokenCount = _tokenizer.CountTokens(content, considerNormalization: false);
        return new IngestionChunk<string>(content, document, tokenCount);
    }
}

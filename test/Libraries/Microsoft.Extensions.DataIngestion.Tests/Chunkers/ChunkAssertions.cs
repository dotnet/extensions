// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public static class ChunkAssertions
    {
        public static void ContentEquals(string expected, IngestionChunk<string> chunk)
        {
            Assert.Equal(expected, chunk.Content.Trim(), ignoreLineEndingDifferences: true);
        }

        public static void ContextEquals(string expected, IngestionChunk<string> chunk)
        {
            Assert.Equal(expected, chunk.Context?.Trim() ?? string.Empty);
        }
    }
}

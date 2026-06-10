// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public class OverlapTokenChunkerTests : DocumentTokenChunkerTests
    {
        protected override IngestionChunker CreateDocumentChunker(int maxTokensPerChunk = 2_000, int overlapTokens = 500)
        {
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            return new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = overlapTokens });
        }

        [Fact]
        public async Task TokenChunking_WithOverlap()
        {
            string text = "The quick brown fox jumps over the lazy dog";
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            int chunkSize = 4;  // Small chunk size to demonstrate overlap
            int chunkOverlap = 1;

            var chunker = new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = chunkSize, OverlapTokens = chunkOverlap });
            IngestionDocument doc = new IngestionDocument("overlapExample");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });

            IReadOnlyList<IngestionChunk> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Equal(3, chunks.Count);
            Assert.Equal("The quick brown fox", ((TextContent)chunks[0].Content).Text, ignoreLineEndingDifferences: true);
            Assert.Equal(" fox jumps over the", ((TextContent)chunks[1].Content).Text, ignoreLineEndingDifferences: true);
            Assert.Equal(" the lazy dog", ((TextContent)chunks[2].Content).Text, ignoreLineEndingDifferences: true);

            Assert.True(tokenizer.CountTokens(((TextContent)chunks.Last().Content).Text!) <= chunkSize);

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var currentChunk = chunks[i];
                var nextChunk = chunks[i + 1];

                var currentWords = ((TextContent)currentChunk.Content).Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var nextWords = ((TextContent)nextChunk.Content).Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                bool hasOverlap = currentWords.Intersect(nextWords).Any();
                Assert.True(hasOverlap, $"Chunks {i} and {i + 1} should have overlapping content");
            }

            Assert.NotEmpty(string.Concat(chunks.Select(c => ((TextContent)c.Content).Text)));
        }
    }
}

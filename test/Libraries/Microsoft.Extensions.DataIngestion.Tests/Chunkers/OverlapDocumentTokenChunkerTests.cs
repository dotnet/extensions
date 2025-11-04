// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public class OverlapDocumentTokenChunkerTests : DocumentTokenChunkerTests
    {
        protected override IngestionChunker<string> CreateDocumentChunker(int maxTokensPerChunk = 2_000, int overlapTokens = 500)
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
            var chunker = new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = chunkSize, OverlapTokens = 1 });
            IngestionDocument doc = new IngestionDocument("overlapExample");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });

            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Equal(3, chunks.Count);
            ChunkAssertions.ContentEquals("The quick brown fox", chunks[0]);
            ChunkAssertions.ContentEquals("fox jumps over the", chunks[1]);
            ChunkAssertions.ContentEquals("the lazy dog", chunks[2]);

            foreach (var chunk in chunks)
            {
                Assert.True(tokenizer.CountTokens(chunks.Last().Content) <= chunkSize);
            }

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var currentChunk = chunks[i];
                var nextChunk = chunks[i + 1];

                var currentWords = currentChunk.Content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var nextWords = nextChunk.Content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                bool hasOverlap = currentWords.Intersect(nextWords).Any();
                Assert.True(hasOverlap, $"Chunks {i} and {i + 1} should have overlapping content");
            }

            Assert.NotEmpty(string.Concat(chunks.Select(c => c.Content)));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public class NoOverlapTokenChunkerTests : DocumentTokenChunkerTests
    {
        protected override IngestionChunker<string> CreateDocumentChunker(int maxTokensPerChunk = 2_000, int overlapTokens = 500)
        {
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            return new DocumentTokenChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = 0 });
        }

        [Fact]
        public async Task TwoChunks()
        {
            string text = string.Join(" ", Enumerable.Repeat("word", 600)); // each word is 1 token
            IngestionDocument doc = new IngestionDocument("twoChunksNoOverlapDoc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });
            IngestionChunker<string> chunker = CreateDocumentChunker(maxTokensPerChunk: 512);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Equal(2, chunks.Count);
            Assert.True(chunks[0].Content.Split(' ').Length <= 512);
            Assert.True(chunks[1].Content.Split(' ').Length <= 512);
            Assert.Equal(text, string.Join("", chunks.Select(c => c.Content)));
        }

        [Fact]
        public async Task ManyChunks()
        {
            string text = string.Join(" ", Enumerable.Repeat("word", 1500)); // each word is 1 token
            IngestionDocument doc = new IngestionDocument("smallChunksNoOverlapDoc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });

            IngestionChunker<string> chunker = CreateDocumentChunker(maxTokensPerChunk: 200, overlapTokens: 0);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Equal(8, chunks.Count);
            foreach (var chunk in chunks)
            {
                Assert.True(chunk.Content.Split(' ').Count(str => str.Contains("word")) <= 200);
            }

            Assert.Equal(text, string.Join("", chunks.Select(c => c.Content)));
        }

        [Fact]
        public async Task VerifyTokenCount()
        {
            string text = string.Join(" ", Enumerable.Repeat("word", 600)); // each word is 1 token
            IngestionDocument doc = new IngestionDocument("verifyTokenCountDoc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });

            Tokenizer tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            IngestionChunker<string> chunker = CreateDocumentChunker(maxTokensPerChunk: 512, overlapTokens: 0);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

            Assert.Equal(2, chunks.Count);
            foreach (IngestionChunk<string> chunk in chunks)
            {
                // Verify that TokenCount property is set
                Assert.True(chunk.TokenCount > 0);

                // Verify that TokenCount matches actual token count of content
                int actualTokenCount = tokenizer.CountTokens(chunk.Content, considerNormalization: false);
                Assert.Equal(actualTokenCount, chunk.TokenCount);

                // Verify that TokenCount does not exceed max tokens per chunk
                Assert.True(chunk.TokenCount <= 512);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public class SectionChunkerTests : DocumentChunkerTests
    {
        protected override IngestionChunker<string> CreateDocumentChunker(int maxTokensPerChunk = 2_000, int overlapTokens = 500)
        {
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            return new SectionChunker(new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = overlapTokens });
        }

        [Fact]
        public async Task OneSection()
        {
            IngestionDocument doc = new IngestionDocument("doc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph("This is a paragraph."),
                    new IngestionDocumentParagraph("This is another paragraph.")
                }
            });
            IngestionChunker<string> chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Single(chunks);
            string expectedResult = "This is a paragraph.\nThis is another paragraph.";
            Assert.Equal(expectedResult, chunks[0].Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task TwoSections()
        {
            IngestionDocument doc = new("doc")
            {
                Sections =
                {
                    new()
                    {
                        Elements =
                        {
                            new IngestionDocumentParagraph("This is a paragraph."),
                            new IngestionDocumentParagraph("This is another paragraph.")
                        }
                    },
                    new()
                    {
                        Elements =
                        {
                            new IngestionDocumentParagraph("This is a paragraph in section 2."),
                            new IngestionDocumentParagraph("This is another paragraph in section 2.")
                        }
                    }
                }
            };

            IngestionChunker<string> chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

            Assert.Equal(2, chunks.Count);
            string expectedResult1 = "This is a paragraph.\nThis is another paragraph.";
            string expectedResult2 = "This is a paragraph in section 2.\nThis is another paragraph in section 2.";
            Assert.Equal(expectedResult1, chunks[0].Content, ignoreLineEndingDifferences: true);
            Assert.Equal(expectedResult2, chunks[1].Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task EmptySection()
        {
            IngestionDocument doc = new IngestionDocument("doc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements = { }
            });
            IngestionChunker<string> chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Empty(chunks);
        }

        [Fact]
        public async Task NestedSections()
        {
            IngestionDocument doc = new("doc")
            {
                Sections =
                {
                    new()
                    {
                        Elements =
                        {
                            new IngestionDocumentHeader("# Section title"),
                            new IngestionDocumentParagraph("This is a paragraph in section 1."),
                            new IngestionDocumentParagraph("This is another paragraph in section 1."),
                            new IngestionDocumentSection
                            {
                                Elements =
                                {
                                    new IngestionDocumentHeader("## Subsection title"),
                                    new IngestionDocumentParagraph("This is a paragraph in subsection 1.1."),
                                    new IngestionDocumentParagraph("This is another paragraph in subsection 1.1."),
                                    new IngestionDocumentSection
                                    {
                                        Elements =
                                        {
                                            new IngestionDocumentHeader("### Subsubsection title"),
                                            new IngestionDocumentParagraph("This is a paragraph in subsubsection 1.1.1."),
                                            new IngestionDocumentParagraph("This is another paragraph in subsubsection 1.1.1.")
                                        }
                                    },
                                    new IngestionDocumentParagraph("This is last paragraph in subsection 1.2."),
                                }
                            }
                        }
                    }
                }
            };

            IngestionChunker<string> chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

            Assert.Equal(4, chunks.Count);
            Assert.Equal("# Section title", chunks[0].Context);
            Assert.Equal("# Section title\nThis is a paragraph in section 1.\nThis is another paragraph in section 1.",
                chunks[0].Content, ignoreLineEndingDifferences: true);
            Assert.Equal("# Section title ## Subsection title", chunks[1].Context);
            Assert.Equal("# Section title ## Subsection title\nThis is a paragraph in subsection 1.1.\nThis is another paragraph in subsection 1.1.",
                chunks[1].Content, ignoreLineEndingDifferences: true);
            Assert.Equal("# Section title ## Subsection title ### Subsubsection title", chunks[2].Context);
            Assert.Equal("# Section title ## Subsection title ### Subsubsection title\nThis is a paragraph in subsubsection 1.1.1.\nThis is another paragraph in subsubsection 1.1.1.",
                chunks[2].Content, ignoreLineEndingDifferences: true);
            Assert.Equal("# Section title ## Subsection title", chunks[3].Context);
            Assert.Equal("# Section title ## Subsection title\nThis is last paragraph in subsection 1.2.", chunks[3].Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task SizeLimit_TwoChunks()
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
            Assert.Equal(text, string.Join("", chunks.Select(c => c.Content)), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task SectionWithHeader()
        {
            IngestionDocument doc = new IngestionDocument("doc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentHeader("Section 1"),
                    new IngestionDocumentParagraph("This is a paragraph in section 1."),
                    new IngestionDocumentParagraph("This is another paragraph in section 1.")
                }
            });
            IngestionChunker<string> chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            IngestionChunk<string> chunk = Assert.Single(chunks);
            string expectedResult = "Section 1\nThis is a paragraph in section 1.\nThis is another paragraph in section 1.";
            Assert.Equal(expectedResult, chunk.Content, ignoreLineEndingDifferences: true);
            Assert.Equal("Section 1", chunk.Context);
        }
    }
}

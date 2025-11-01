// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public class SemanticSimilarityChunkerTests : DocumentChunkerTests
    {
        protected override IngestionChunker<string> CreateDocumentChunker(int maxTokensPerChunk = 2_000, int overlapTokens = 500)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            TestEmbeddingGenerator embeddingClient = new();
#pragma warning restore CA2000 // Dispose objects before losing scope
            return CreateSemanticSimilarityChunker(embeddingClient, maxTokensPerChunk, overlapTokens);
        }

        private static IngestionChunker<string> CreateSemanticSimilarityChunker(TestEmbeddingGenerator embeddingClient, int maxTokensPerChunk = 2_000, int overlapTokens = 500)
        {
            Tokenizer tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
            return new SemanticSimilarityChunker(embeddingClient,
                new(tokenizer) { MaxTokensPerChunk = maxTokensPerChunk, OverlapTokens = overlapTokens });
        }

        [Fact]
        public async Task SingleParagraph()
        {
            string text = ".NET is a free, cross-platform, open-source developer platform for building many " +
                "kinds of applications. It can run programs written in multiple languages, with C# being the most popular. " +
                "It relies on a high-performance runtime that is used in production by many high-scale apps.";
            IngestionDocument doc = new IngestionDocument("doc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });
            using var customGenerator = new TestEmbeddingGenerator
            {
                GenerateAsyncCallback = static async (values, options, ct) =>
                {
                    var embeddings = values.Select(v =>
                        new Embedding<float>(new float[] { 1.0f, 2.0f, 3.0f, 4.0f }))
                        .ToArray();

                    return [.. embeddings];
                }
            };
            IngestionChunker<string> chunker = CreateSemanticSimilarityChunker(customGenerator);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Single(chunks);
            Assert.Equal(text, chunks[0].Content);
        }

        [Fact]
        public async Task TwoTopicsParagraphs()
        {
            IngestionDocument doc = new IngestionDocument("doc");
            string text1 = ".NET is a free, cross-platform, open-source developer platform for building many" +
                "kinds of applications. It can run programs written in multiple languages, with C# being the most popular.";
            string text2 = "It relies on a high-performance runtime that is used in production by many high-scale apps.";
            string text3 = "Zeus is the chief deity of the Greek pantheon. He is a sky and thunder god in ancient Greek religion and mythology.";
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text1),
                    new IngestionDocumentParagraph(text2),
                    new IngestionDocumentParagraph(text3)
                }
            });

            using var customGenerator = new TestEmbeddingGenerator
            {
                GenerateAsyncCallback = async (values, options, ct) =>
                {
                    var embeddings = values.Select((_, index) =>
                    {
                        return index switch
                        {
                            0 => new Embedding<float>(new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                            1 => new Embedding<float>(new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                            2 => new Embedding<float>(new float[] { -1.0f, -1.0f, -1.0f, -1.0f }),
                            _ => throw new InvalidOperationException("Unexpected call count")
                        };
                    }).ToArray();

                    return [.. embeddings];
                }
            };

            IngestionChunker<string> chunker = CreateSemanticSimilarityChunker(customGenerator);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();
            Assert.Equal(2, chunks.Count);
            Assert.Equal(text1 + Environment.NewLine + text2, chunks[0].Content);
            Assert.Equal(text3, chunks[1].Content);
        }

        [Fact]
        public async Task TwoSeparateTopicsWithAllKindsOfElements()
        {
            string dotNetTableMarkdown = @"| Language | Type | Status |
| --- | --- | --- |
| C# | Object-oriented | Primary |
| F# | Functional | Official |
| Visual Basic | Object-oriented | Official |
| PowerShell | Scripting | Supported |
| IronPython | Dynamic | Community |
| IronRuby | Dynamic | Community |
| Boo | Object-oriented | Community |
| Nemerle | Functional/OOP | Community |";

            string godsTableMarkdown = @"| God | Domain | Symbol | Roman Name |
| --- | --- | --- | --- |
| Zeus | Sky & Thunder | Lightning Bolt | Jupiter |
| Hera | Marriage & Family | Peacock | Juno |
| Poseidon | Sea & Earthquakes | Trident | Neptune |
| Athena | Wisdom & War | Owl | Minerva |
| Apollo | Sun & Music | Lyre | Apollo |
| Artemis | Hunt & Moon | Silver Bow | Diana |
| Aphrodite | Love & Beauty | Dove | Venus |
| Ares | War & Courage | Spear | Mars |
| Hephaestus | Fire & Forge | Hammer | Vulcan |
| Demeter | Harvest & Nature | Wheat | Ceres |
| Dionysus | Wine & Festivity | Grapes | Bacchus |
| Hermes | Messages & Trade | Caduceus | Mercury |";

            IngestionDocument doc = new("dotnet-languages");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentHeader("# .NET Supported Languages") { Level = 1 },
                    new IngestionDocumentParagraph("The .NET platform supports multiple programming languages:"),
                    new IngestionDocumentTable(dotNetTableMarkdown,
                        ToParagraphCells(CreateLanguageTableCells())),
                    new IngestionDocumentParagraph("C# remains the most popular language for .NET development."),
                    new IngestionDocumentHeader("# Ancient Greek Olympian Gods") { Level = 1 },
                    new IngestionDocumentParagraph("The twelve Olympian gods were the principal deities of the Greek pantheon:"),
                    new IngestionDocumentTable(godsTableMarkdown,
                        ToParagraphCells(CreateGreekGodsTableCells())),
                    new IngestionDocumentParagraph("These gods resided on Mount Olympus and ruled over different aspects of mortal and divine life.")
                }
            });

            using var customGenerator = new TestEmbeddingGenerator
            {
                GenerateAsyncCallback = async (values, options, ct) =>
                {
                    var embeddings = values.Select((_, index) =>
                    {
                        return index switch
                        {
                            <= 3 => new Embedding<float>(new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                            >= 4 and <= 7 => new Embedding<float>(new float[] { -1.0f, -1.0f, -1.0f, -1.0f }),
                            _ => throw new InvalidOperationException($"Unexpected index: {index}")
                        };
                    }).ToArray();

                    return [.. embeddings];
                }
            };

            IngestionChunker<string> chunker = CreateSemanticSimilarityChunker(customGenerator, 200, 0);
            IReadOnlyList<IngestionChunk<string>> chunks = await chunker.ProcessAsync(doc).ToListAsync();

            Assert.Equal(3, chunks.Count);
            Assert.All(chunks, chunk => Assert.Same(doc, chunk.Document));
            Assert.Equal($@"# .NET Supported Languages
The .NET platform supports multiple programming languages:
{dotNetTableMarkdown}
C# remains the most popular language for .NET development.",
            chunks[0].Content, ignoreLineEndingDifferences: true);
            Assert.Equal($@"# Ancient Greek Olympian Gods
The twelve Olympian gods were the principal deities of the Greek pantheon:
| God | Domain | Symbol | Roman Name |
| --- | --- | --- | --- |
| Zeus | Sky & Thunder | Lightning Bolt | Jupiter |
| Hera | Marriage & Family | Peacock | Juno |
| Poseidon | Sea & Earthquakes | Trident | Neptune |
| Athena | Wisdom & War | Owl | Minerva |
| Apollo | Sun & Music | Lyre | Apollo |
| Artemis | Hunt & Moon | Silver Bow | Diana |
| Aphrodite | Love & Beauty | Dove | Venus |
| Ares | War & Courage | Spear | Mars |
| Hephaestus | Fire & Forge | Hammer | Vulcan |
| Demeter | Harvest & Nature | Wheat | Ceres |
| Dionysus | Wine & Festivity | Grapes | Bacchus |",
            chunks[1].Content, ignoreLineEndingDifferences: true);
            Assert.Equal($@"| God | Domain | Symbol | Roman Name |
| --- | --- | --- | --- |
| Hermes | Messages & Trade | Caduceus | Mercury |
These gods resided on Mount Olympus and ruled over different aspects of mortal and divine life.",
            chunks[2].Content, ignoreLineEndingDifferences: true);

            static string[,] CreateGreekGodsTableCells() => new string[,]
                {
                    { "God", "Domain", "Symbol", "Roman Name" },
                    { "Zeus", "Sky & Thunder", "Lightning Bolt", "Jupiter" },
                    { "Hera", "Marriage & Family", "Peacock", "Juno" },
                    { "Poseidon", "Sea & Earthquakes", "Trident", "Neptune" },
                    { "Athena", "Wisdom & War", "Owl", "Minerva" },
                    { "Apollo", "Sun & Music", "Lyre", "Apollo" },
                    { "Artemis", "Hunt & Moon", "Silver Bow", "Diana" },
                    { "Aphrodite", "Love & Beauty", "Dove", "Venus" },
                    { "Ares", "War & Courage", "Spear", "Mars" },
                    { "Hephaestus", "Fire & Forge", "Hammer", "Vulcan" },
                    { "Demeter", "Harvest & Nature", "Wheat", "Ceres" },
                    { "Dionysus", "Wine & Festivity", "Grapes", "Bacchus" },
                    { "Hermes", "Messages & Trade", "Caduceus", "Mercury" }
                };

            static string[,] CreateLanguageTableCells() => new string[,]
                {
                    { "Language", "Type", "Status" },
                    { "C#", "Object-oriented", "Primary" },
                    { "F#", "Functional", "Official" },
                    { "Visual Basic", "Object-oriented", "Official" },
                    { "PowerShell", "Scripting", "Supported" },
                    { "IronPython", "Dynamic", "Community" },
                    { "IronRuby", "Dynamic", "Community" },
                    { "Boo", "Object-oriented", "Community" },
                    { "Nemerle", "Functional/OOP", "Community" }
                };
        }

        private static IngestionDocumentParagraph?[,] ToParagraphCells(string[,] cells)
        {
            int rows = cells.GetLength(0);
            int cols = cells.GetLength(1);
            var result = new IngestionDocumentParagraph?[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = new IngestionDocumentParagraph(cells[i, j]);
                }
            }

            return result;
        }
    }
}

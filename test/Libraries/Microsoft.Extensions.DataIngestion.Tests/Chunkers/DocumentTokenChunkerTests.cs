// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Chunkers.Tests
{
    public abstract class DocumentTokenChunkerTests : DocumentChunkerTests
    {
        protected static string GetText(IngestionChunk chunk) => ((TextContent)chunk.Content).Text!;

        [Fact]
        public async Task SingleChunkText()
        {
            string text = "This is a short document that fits within a single chunk.";
            IngestionDocument doc = new IngestionDocument("singleChunkDoc");
            doc.Sections.Add(new IngestionDocumentSection
            {
                Elements =
                {
                    new IngestionDocumentParagraph(text)
                }
            });

            IngestionChunker chunker = CreateDocumentChunker();
            IReadOnlyList<IngestionChunk> chunks = await chunker.ProcessAsync(doc).ToListAsync();

            IngestionChunk chunk = Assert.Single(chunks);
            Assert.Equal(text, GetText(chunk), ignoreLineEndingDifferences: true);
        }
    }
}

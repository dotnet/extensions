// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion.Chunkers
{
    /// <summary>
    /// Processes a Markdown document and splits it into smaller chunks based on specified header levels.
    /// </summary>
    /// <remarks>This class is designed to parse a Markdown document and divide it into logical chunks based
    /// on the specified header level. Each chunk represents a section of the document, and the headers can be
    /// optionally stripped from the output. The splitting behavior is controlled by the header level. </remarks>
    public sealed class MarkdownChunker : IngestionChunker<string>
    {
        private readonly int _headerLevelToSplitOn;
        private readonly bool _stripHeaders;
        private static string StringyfyContext(string? context, string? lastHeader)
        {
            return string.Join(";", new[] { context, lastHeader }.Where(x => x is not null));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownChunker"/> class.
        /// </summary>
        /// <param name="headerLevelToSplitOn">The Markdown header level to split the document on.</param>
        /// <param name="stripHeaders">Flag indicating whether to strip headers from the output chunks.</param>
        public MarkdownChunker(int headerLevelToSplitOn = 3, bool stripHeaders = true)
        {
            _headerLevelToSplitOn = headerLevelToSplitOn;
            _stripHeaders = stripHeaders;
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = Throw.IfNull(document);

            IEnumerable<IngestionDocumentElement> elements = document.EnumerateContent().Reverse();
            var sectionStack = new Stack<IngestionDocumentElement>(elements);

            var chunks = ParseLevel(document, sectionStack, 1);
            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }

        private List<IngestionChunk<string>> ParseLevel(IngestionDocument document, Stack<IngestionDocumentElement> lines, int markdownHeaderLevel, string? context = null, string? lastHeader = null)
        {
            List<IngestionChunk<string>> chunks = [];

            StringBuilder sb = new StringBuilder();

            while (lines.Any())
            {
                IngestionDocumentElement element = lines.Pop();

                int headerLevel = element is IngestionDocumentHeader header ? header.Level.GetValueOrDefault(0) : 0;
                if (headerLevel == 0 || headerLevel > _headerLevelToSplitOn)
                {
                    _ = sb.AppendLine(element.GetMarkdown());
                }
                else
                {
                    IngestionChunk<string>? currentChunk = CreateChunk(document, sb, context, lastHeader);
                    if (currentChunk is not null)
                    {
                        chunks.Add(currentChunk);
                    }
                    _ = sb.Clear();

                    if (headerLevel == markdownHeaderLevel)
                    {
                        lastHeader = element.GetMarkdown();
                    }
                    else if (headerLevel < markdownHeaderLevel)
                    {
                        lines.Push(element);
                        return chunks;
                    }
                    else
                    {
                        string newContext = StringyfyContext(context, lastHeader);
                        chunks.AddRange(ParseLevel(document, lines, markdownHeaderLevel + 1, newContext, element.GetMarkdown()));
                    }

                }
            }

            IngestionChunk<string>? chunk = CreateChunk(document, sb, context, lastHeader);
            if (chunk is not null)
            {
                chunks.Add(chunk);
            }

            return chunks;
        }

        private IngestionChunk<string>? CreateChunk(IngestionDocument document, StringBuilder content, string? context, string? header)
        {
            context = StringyfyContext(context, header);
            if (!_stripHeaders)
            {
                _ = content.Insert(0, context);
            }
            string textContent = content.ToString();

            if (string.IsNullOrWhiteSpace(textContent))
            {
                return null;
            }
            return new(textContent, document, context: context);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.ML.Tokenizers;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion.Chunkers
{
    /// <summary>
    /// Processes a document by tokenizing its content and dividing it into overlapping chunks of tokens.
    /// </summary>
    /// <remarks>This class uses a tokenizer to convert the document's content into tokens and then splits the
    /// tokens into chunks of a specified size, with a configurable overlap between consecutive chunks.</remarks>
    public sealed class DocumentTokenChunker : IngestionChunker<string>
    {
        private readonly Tokenizer _tokenizer;
        private readonly int _maxTokensPerChunk;
        private readonly int _chunkOverlap;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTokenChunker"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options used to configure the chunker, including tokenizer and chunk sizes.</param>
        public DocumentTokenChunker(IngestionChunkerOptions options)
        {
            _ = Throw.IfNull(options);

            _tokenizer = options.Tokenizer;
            _maxTokensPerChunk = options.MaxTokensPerChunk;
            _chunkOverlap = options.OverlapTokens;
        }

        /// <inheritdoc/>
        public override IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
        {
            _ = Throw.IfNull(document);

            string documentMarkdown = GetDocumentMarkdown(document);
            int[] tokens = _tokenizer.EncodeToIds(documentMarkdown).ToArray();
            List<ArraySegment<int>> tokenGroups = CreateGroups(tokens);

            return tokenGroups.Select(g => GroupToChunk(document, g)).ToAsyncEnumerable();
        }

        private static string GetDocumentMarkdown(IngestionDocument document)
        {
            StringBuilder sb = new();
            for (int i = 0; i < document.Sections.Count; i++)
            {
                _ = sb.Append(document.Sections[i].GetMarkdown());
                if (i != document.Sections.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private List<ArraySegment<int>> CreateGroups(int[] tokens)
        {
            List<ArraySegment<int>> groups = [];
            for (int i = 0; i < tokens.Length; i += (_maxTokensPerChunk - _chunkOverlap))
            {
                int count = Math.Min(_maxTokensPerChunk, tokens.Length - i);
                groups.Add(new ArraySegment<int>(tokens, i, count));
            }
            return groups;
        }

        private IngestionChunk<string> GroupToChunk(IngestionDocument document, ArraySegment<int> tokenGroup)
        {
            string text = _tokenizer.Decode(tokenGroup);
            return new(text, document);
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.ML.Tokenizers;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion.Chunkers
{
    /// <summary>
    /// Processes a document by tokenizing its content and dividing it into overlapping chunks of tokens.
    /// </summary>
    /// <remarks>
    /// <para>This class uses a tokenizer to convert the document's content into tokens and then splits the
    /// tokens into chunks of a specified size, with a configurable overlap between consecutive chunks.</para>
    /// <para>Note that tables may be split mid-row.</para>
    /// </remarks>
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
        public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = Throw.IfNull(document);

            int prevOverlapTokenCount = 0;
            string? prevOverlap = string.Empty;
            int stringBuilderTokenCount = 0;
            StringBuilder stringBuilder = new();
            foreach (IngestionDocumentSection section in document.Sections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? semanticContentToProcess = section.GetSemanticContent();
                if (string.IsNullOrEmpty(semanticContentToProcess))
                {
                    continue;
                }

                int elementTokenCount = _tokenizer.CountTokens(semanticContentToProcess!, considerNormalization: false);
                while (prevOverlapTokenCount + stringBuilderTokenCount + elementTokenCount > _maxTokensPerChunk)
                {
                    int index = _tokenizer.GetIndexByTokenCount(
                        text: semanticContentToProcess!,
                        maxTokenCount: _maxTokensPerChunk - prevOverlapTokenCount - stringBuilderTokenCount,
                        out string? _,
                        out int _,
                        considerNormalization: false);

                    ReadOnlySpan<char> spanToAppend = semanticContentToProcess.AsSpan(0, index);
#if NET
                    stringBuilder.Append(spanToAppend);
#else
                    stringBuilder.Append(spanToAppend.ToString());
#endif
                    yield return FinaliseChunk();

                    semanticContentToProcess = semanticContentToProcess!.Substring(index);
                    elementTokenCount = _tokenizer.CountTokens(semanticContentToProcess);

                }

                stringBuilder.Append(semanticContentToProcess);
                stringBuilderTokenCount += elementTokenCount;
            }

            if (stringBuilder.Length > 0)
            {
                yield return FinaliseChunk();
            }
            yield break;

            IngestionChunk<string> FinaliseChunk()
            {
                stringBuilder.Insert(0, prevOverlap);
                IngestionChunk<string> chunk = new IngestionChunk<string>(
                    content: stringBuilder.ToString(),
                    document: document,
                    context: string.Empty);
                stringBuilder.Clear();
                stringBuilderTokenCount = 0;

                if (_chunkOverlap > 0)
                {
                    int index = _tokenizer.GetIndexByTokenCountFromEnd(
                        text: chunk.Content,
                        maxTokenCount: _chunkOverlap,
                        out string? _,
                        out prevOverlapTokenCount,
                        considerNormalization: false);
                    prevOverlap = chunk.Content.Substring(index);
                }

                return chunk;
            }
        }

    }
}

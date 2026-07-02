// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.AI;
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
    public sealed class DocumentTokenChunker : IngestionChunker
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
        public override async IAsyncEnumerable<IngestionChunk> ProcessAsync(IngestionDocument document, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = Throw.IfNull(document);

            int stringBuilderTokenCount = 0;
            StringBuilder stringBuilder = new();
            Dictionary<string, object>? accumulatedMetadata = null;
            foreach (IngestionDocumentElement element in document.EnumerateContent())
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? elementContent = element.GetSemanticContent();
                if (string.IsNullOrEmpty(elementContent))
                {
                    continue;
                }

                int contentToProcessTokenCount = _tokenizer.CountTokens(elementContent!, considerNormalization: false);
                ReadOnlyMemory<char> contentToProcess = elementContent.AsMemory();
                bool elementMetadataAccumulated = false;
                while (stringBuilderTokenCount + contentToProcessTokenCount >= _maxTokensPerChunk)
                {
                    int index = _tokenizer.GetIndexByTokenCount(
                        text: contentToProcess.Span,
                        maxTokenCount: _maxTokensPerChunk - stringBuilderTokenCount,
                        out string? _,
                        out int addedTokenCount,
                        considerNormalization: false);

                    // Accumulate metadata the first time this element contributes content.
                    if (!elementMetadataAccumulated && index > 0)
                    {
                        AccumulateMetadata(element, ref accumulatedMetadata);
                        elementMetadataAccumulated = true;
                    }

                    unsafe
                    {
                        fixed (char* ptr = &MemoryMarshal.GetReference(contentToProcess.Span))
                        {
                            _ = stringBuilder.Append(ptr, index);
                        }
                    }
                    stringBuilderTokenCount += addedTokenCount;
                    yield return FinalizeChunk(ref accumulatedMetadata);

                    contentToProcess = contentToProcess.Slice(index);
                    contentToProcessTokenCount = _tokenizer.CountTokens(contentToProcess.Span, considerNormalization: false);
                }

                // Accumulate metadata if the element only contributed content after the loop.
                if (!elementMetadataAccumulated)
                {
                    AccumulateMetadata(element, ref accumulatedMetadata);
                }

                _ = stringBuilder.Append(contentToProcess);
                stringBuilderTokenCount += contentToProcessTokenCount;
            }

            if (stringBuilder.Length > 0)
            {
                yield return FinalizeChunk(ref accumulatedMetadata);
            }
            yield break;

            IngestionChunk FinalizeChunk(ref Dictionary<string, object>? metadata)
            {
                TextContent chunkContent = new(stringBuilder.ToString());
                IngestionChunk chunk = new IngestionChunk(
                    content: chunkContent,
                    document: document,
                    tokenCount: stringBuilderTokenCount,
                    context: string.Empty);

                if (metadata is { Count: > 0 })
                {
                    foreach (var kvp in metadata)
                    {
                        chunk.Metadata[kvp.Key] = kvp.Value;
                    }

                    metadata = null;
                }

                _ = stringBuilder.Clear();
                stringBuilderTokenCount = 0;

                if (_chunkOverlap > 0)
                {
                    string chunkText = chunkContent.Text;
                    int index = _tokenizer.GetIndexByTokenCountFromEnd(
                        text: chunkText,
                        maxTokenCount: _chunkOverlap,
                        out string? _,
                        out stringBuilderTokenCount,
                        considerNormalization: false);

                    ReadOnlySpan<char> overlapContent = chunkText.AsSpan().Slice(index);
                    unsafe
                    {
                        fixed (char* ptr = &MemoryMarshal.GetReference(overlapContent))
                        {
                            _ = stringBuilder.Append(ptr, overlapContent.Length);
                        }
                    }
                }

                return chunk;
            }
        }

        private static void AccumulateMetadata(IngestionDocumentElement element, ref Dictionary<string, object>? accumulated)
        {
            if (!element.HasMetadata)
            {
                return;
            }

            accumulated ??= [];
            foreach (var kvp in element.Metadata)
            {
                if (kvp.Value is not null)
                {
#if NET
                    accumulated.TryAdd(kvp.Key, kvp.Value);
#else
                    if (!accumulated.ContainsKey(kvp.Key))
                    {
                        accumulated[kvp.Key] = kvp.Value;
                    }
#endif
                }
            }
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DataIngestion.Chunkers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Splits documents into chunks based on headers and their corresponding levels, preserving the header context.
/// </summary>
public sealed class HeaderChunker : IngestionChunker<string>
{
    private const int MaxHeaderLevel = 10;
    private readonly ElementsChunker _elementsChunker;

    public HeaderChunker(IngestionChunkerOptions options)
        => _elementsChunker = new(options);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document,
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<IngestionDocumentElement> elements = new(20);
        string?[] headers = new string?[MaxHeaderLevel + 1];

        foreach (IngestionDocumentElement element in document.EnumerateContent())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (element is IngestionDocumentHeader header)
            {
                foreach (var chunk in SplitIntoChunks(document, headers, elements))
                {
                    yield return chunk;
                }

                int headerLevel = header.Level.GetValueOrDefault();
                headers[headerLevel] = header.GetMarkdown();
                headers.AsSpan(headerLevel + 1).Clear(); // clear all lower level headers

                continue; // don't add headers to the elements list, they are part of the context
            }

            elements.Add(element);
        }

        // take care of any remaining paragraphs
        foreach (var chunk in SplitIntoChunks(document, headers, elements))
        {
            yield return chunk;
        }
    }

    private IEnumerable<IngestionChunk<string>> SplitIntoChunks(IngestionDocument document, string?[] headers, List<IngestionDocumentElement> elements)
    {
        if (elements.Count > 0)
        {
            string chunkHeader = string.Join(" ", headers.Where(h => !string.IsNullOrEmpty(h)));

            foreach (var chunk in _elementsChunker.Process(document, chunkHeader, elements))
            {
                yield return chunk;
            }

            elements.Clear();
        }
    }
}

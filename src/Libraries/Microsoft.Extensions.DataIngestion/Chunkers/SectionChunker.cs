// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion.Chunkers;

/// <summary>
/// Treats each <see cref="IngestionDocumentSection" /> in a <see cref="IngestionDocument.Sections"/> as a separate entity.
/// </summary>
public sealed class SectionChunker : IngestionChunker<string>
{
    private readonly ElementsChunker _elementsChunker;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionChunker"/> class.
    /// </summary>
    /// <param name="options">The options for the chunker.</param>
    public SectionChunker(IngestionChunkerOptions options)
    {
        _elementsChunker = new(options);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(document);

        List<IngestionChunk<string>> chunks = [];
        foreach (IngestionDocumentSection section in document.Sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Process(document, section, chunks);
        }

        foreach (var chunk in chunks)
        {
            yield return chunk;
        }
    }

    private void Process(IngestionDocument document, IngestionDocumentSection section, List<IngestionChunk<string>> chunks, string? parentContext = null)
    {
        List<IngestionDocumentElement> elements = new(section.Elements.Count);
        string context = parentContext ?? string.Empty;

        for (int i = 0; i < section.Elements.Count; i++)
        {
            switch (section.Elements[i])
            {
                // If the first element is a header, we use it as a context.
                // This is common for various documents and readers.
                case IngestionDocumentHeader documentHeader when i == 0:
                    context = string.IsNullOrEmpty(context)
                        ? documentHeader.GetMarkdown()
                        : context + $" {documentHeader.GetMarkdown()}";
                    break;
                case IngestionDocumentSection nestedSection:
                    Commit();
                    Process(document, nestedSection, chunks, context);
                    break;
                default:
                    elements.Add(section.Elements[i]);
                    break;
            }
        }

        Commit();

        void Commit()
        {
            if (elements.Count > 0)
            {
                foreach (var chunk in _elementsChunker.Process(document, context, elements))
                {
                    chunks.Add(chunk);
                }
                elements.Clear();
            }
        }
    }
}

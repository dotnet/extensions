// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        foreach (IngestionDocumentSection section in document.Sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var chunk in ProcessSectionAsync(document, section, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
    }

    private async IAsyncEnumerable<IngestionChunk<string>> ProcessSectionAsync(IngestionDocument document, IngestionDocumentSection section, [EnumeratorCancellation] CancellationToken cancellationToken, string? parentContext = null)
    {
        List<IngestionDocumentElement> elements = new(section.Elements.Count);
        string context = parentContext ?? string.Empty;

        for (int i = 0; i < section.Elements.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                    await foreach (var chunk in CommitAsync().ConfigureAwait(false))
                    {
                        yield return chunk;
                    }
                    await foreach (var chunk in ProcessSectionAsync(document, nestedSection, cancellationToken, context).ConfigureAwait(false))
                    {
                        yield return chunk;
                    }
                    break;
                default:
                    elements.Add(section.Elements[i]);
                    break;
            }
        }

        await foreach (var chunk in CommitAsync().ConfigureAwait(false))
        {
            yield return chunk;
        }

        async IAsyncEnumerable<IngestionChunk<string>> CommitAsync()
        {
            if (elements.Count > 0)
            {
                foreach (var chunk in _elementsChunker.Process(document, context, elements))
                {
                    yield return chunk;
                }
                elements.Clear();
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}

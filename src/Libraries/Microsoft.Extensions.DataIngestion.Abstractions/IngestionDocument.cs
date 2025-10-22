// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// A format-agnostic container that normalizes diverse input formats into a structured hierarchy.
/// </summary>
public sealed class IngestionDocument
{
    public IngestionDocument(string identifier)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
    }

    public string Identifier { get; }

    public List<IngestionDocumentSection> Sections { get; } = [];

    /// <summary>
    /// Iterate over all elements in the document, including those in nested sections.
    /// </summary>
    /// <remarks>
    /// Sections themselves are not included.
    /// </remarks>
    public IEnumerable<IngestionDocumentElement> EnumerateContent()
    {
        Stack<IngestionDocumentElement> elementsToProcess = new();

        for (int sectionIndex = Sections.Count - 1; sectionIndex >= 0; sectionIndex--)
        {
            elementsToProcess.Push(Sections[sectionIndex]);
        }

        while (elementsToProcess.Count > 0)
        {
            IngestionDocumentElement currentElement = elementsToProcess.Pop();

            if (currentElement is not IngestionDocumentSection nestedSection)
            {
                yield return currentElement;
            }
            else
            {
                for (int i = nestedSection.Elements.Count - 1; i >= 0; i--)
                {
                    elementsToProcess.Push(nestedSection.Elements[i]);
                }
            }
        }
    }
}

[DebuggerDisplay("{GetType().Name}: {GetMarkdown()}")]
public abstract class IngestionDocumentElement
{
    protected string _markdown;

    protected internal IngestionDocumentElement(string markdown)
    {
        _markdown = string.IsNullOrEmpty(markdown) ? throw new ArgumentNullException(nameof(markdown)) : markdown;
    }

    protected internal IngestionDocumentElement() => _markdown = null!;

    private Dictionary<string, object?>? _metadata;

    public string? Text { get; set; }

    public virtual string GetMarkdown() => _markdown;

    public int? PageNumber { get; set; }

    public bool HasMetadata => _metadata?.Count > 0;

    public IDictionary<string, object?> Metadata => _metadata ??= [];
}

/// <summary>
/// A section can be just a page or a logical grouping of elements in a document.
/// </summary>
public sealed class IngestionDocumentSection : IngestionDocumentElement
{
    public IngestionDocumentSection(string markdown) : base(markdown)
    {
    }

    // the user is not providing the Markdown, we will compute it from the elements
    public IngestionDocumentSection() : base()
    {
    }

    public List<IngestionDocumentElement> Elements { get; } = [];

    // The result is not being cached, as elements can be added, removed or modified.
    public override string GetMarkdown()
        => string.Join(Environment.NewLine, Elements.Select(e => e.GetMarkdown()));
}

public sealed class IngestionDocumentParagraph : IngestionDocumentElement
{
    public IngestionDocumentParagraph(string markdown) : base(markdown)
    {
    }
}

public sealed class IngestionDocumentHeader : IngestionDocumentElement
{
    public IngestionDocumentHeader(string markdown) : base(markdown)
    {
    }

    public int? Level { get; set; }
}

public sealed class IngestionDocumentFooter : IngestionDocumentElement
{
    public IngestionDocumentFooter(string markdown) : base(markdown)
    {
    }
}

public sealed class IngestionDocumentTable : IngestionDocumentElement
{
    public IngestionDocumentTable(string markdown, IngestionDocumentElement?[,] cells) : base(markdown)
    {
        Cells = cells ?? throw new ArgumentNullException(nameof(cells));
    }

    /// <summary>
    /// Each table can be represented as a multidimensional array of cell contents, with the first row being the headers.
    /// </summary>
    /// <remarks>
    /// <para>This information is useful when chunking large tables that exceed token count limit.</para>
    /// <para>Null represents an empty cell (<see cref="IngestionDocumentElement.GetMarkdown()"/> can't return an empty string).</para>
    /// </remarks>
    public IngestionDocumentElement?[,] Cells { get; }
}

public sealed class IngestionDocumentImage : IngestionDocumentElement
{
    public IngestionDocumentImage(string markdown) : base(markdown)
    {
    }

    public ReadOnlyMemory<byte>? Content { get; set; }

    public string? MediaType { get; set; }

    /// <summary>
    /// Alternative text is a brief, descriptive text that explains the content, context, or function of an image when the image cannot be displayed or accessed.
    /// This property can be used when generating the embedding for the image that is part of larger chunk.
    /// </summary>
    public string? AlternativeText { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents an element within an <see cref="IngestionDocument"/>.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Markdown = {GetMarkdown()}")]
public abstract class IngestionDocumentElement
{
#pragma warning disable IDE1006 // Naming Styles
    private protected string _markdown;
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentElement"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the element.</param>
    /// <exception cref="ArgumentNullException"><paramref name="markdown"/> is <see langword="null"/> or empty.</exception>
    private protected IngestionDocumentElement(string markdown)
    {
        _markdown = string.IsNullOrEmpty(markdown) ? throw new ArgumentNullException(nameof(markdown)) : markdown;
    }

    private protected IngestionDocumentElement()
    {
        _markdown = null!;
    }

    private Dictionary<string, object?>? _metadata;

    /// <summary>
    /// Gets or sets the textual content of the element.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets the markdown representation of the element.
    /// </summary>
    /// <returns>The markdown representation.</returns>
    public virtual string GetMarkdown() => _markdown;

    /// <summary>
    /// Gets or sets the page number where this element appears.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Gets a value indicating whether this element has metadata.
    /// </summary>
    public bool HasMetadata => _metadata?.Count > 0;

    /// <summary>
    /// Gets the metadata associated with this element.
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata ??= [];
}

/// <summary>
/// A section can be just a page or a logical grouping of elements in a document.
/// </summary>
public sealed class IngestionDocumentSection : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentSection"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the section.</param>
    public IngestionDocumentSection(string markdown)
        : base(markdown)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentSection"/> class.
    /// </summary>
    public IngestionDocumentSection()
    {
    }

    /// <summary>
    /// Gets the elements within this section.
    /// </summary>
    public IList<IngestionDocumentElement> Elements { get; } = [];

    /// <inheritdoc/>
    public override string GetMarkdown()
        => string.Join(Environment.NewLine, Elements.Select(e => e.GetMarkdown()));
}

/// <summary>
/// Represents a paragraph in a document.
/// </summary>
public sealed class IngestionDocumentParagraph : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentParagraph"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the paragraph.</param>
    public IngestionDocumentParagraph(string markdown)
        : base(markdown)
    {
    }
}

/// <summary>
/// Represents a header in a document.
/// </summary>
public sealed class IngestionDocumentHeader : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentHeader"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the header.</param>
    public IngestionDocumentHeader(string markdown)
        : base(markdown)
    {
    }

    /// <summary>
    /// Gets or sets the level of the header.
    /// </summary>
    public int? Level
    {
        get => field;
        set => field = Throw.IfOutOfRange(value.GetValueOrDefault(), min: 0, max: 10, nameof(value));
    }
}

/// <summary>
/// Represents a footer in a document.
/// </summary>
public sealed class IngestionDocumentFooter : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentFooter"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the footer.</param>
    public IngestionDocumentFooter(string markdown)
        : base(markdown)
    {
    }
}

/// <summary>
/// Represents a table in a document.
/// </summary>
public sealed class IngestionDocumentTable : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentTable"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the table.</param>
    /// <param name="cells">The cells of the table.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cells"/> is <see langword="null"/>.</exception>
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable S3967 // Multidimensional arrays should not be used
    public IngestionDocumentTable(string markdown, IngestionDocumentElement?[,] cells)
        : base(markdown)
    {
        Cells = Throw.IfNull(cells);
    }

    /// <summary>
    /// Gets the cells of the table.
    /// Each table can be represented as a two-dimensional array of cell contents, with the first row being the headers.
    /// </summary>
    /// <remarks>
    /// <para>This information is useful when chunking large tables that exceed token count limit.</para>
    /// <para>Null represents an empty cell (<see cref="IngestionDocumentElement.GetMarkdown()"/> can't return an empty string).</para>
    /// </remarks>
#pragma warning disable CA1819 // Properties should not return arrays
    public IngestionDocumentElement?[,] Cells { get; }
#pragma warning restore CA1819 // Properties should not return arrays
#pragma warning restore S3967 // Multidimensional arrays should not be used
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
}

/// <summary>
/// Represents an image in a document.
/// </summary>
public sealed class IngestionDocumentImage : IngestionDocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocumentImage"/> class.
    /// </summary>
    /// <param name="markdown">The markdown representation of the image.</param>
    public IngestionDocumentImage(string markdown)
        : base(markdown)
    {
    }

    /// <summary>
    /// Gets or sets the binary content of the image.
    /// </summary>
    public ReadOnlyMemory<byte>? Content { get; set; }

    /// <summary>
    /// Gets or sets the media type of the image.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the alternative text for the image.
    /// </summary>
    /// <remarks>
    /// Alternative text is a brief, descriptive text that explains the content, context, or function of an image when the image cannot be displayed or accessed.
    /// This property can be used when generating the embedding for the image that is part of larger chunk.
    /// </remarks>
    public string? AlternativeText { get; set; }
}

#pragma warning restore SA1402 // File may only contain a single type

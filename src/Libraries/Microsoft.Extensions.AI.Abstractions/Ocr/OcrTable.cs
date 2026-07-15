// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a table extracted from a document.</summary>
/// <remarks>
/// Cells are the primary, structured representation (row and column indices with spans, the Azure
/// Document Intelligence shape) and are authoritative when non-<see langword="null"/>.
/// <see cref="MarkdownRepresentation"/> is the fallback for engines that only emit markdown or HTML
/// (such as Mistral OCR). Consumers prefer <see cref="Cells"/> when present and fall back to
/// <see cref="MarkdownRepresentation"/> otherwise. On the markdown-only path <see cref="RowCount"/> and
/// <see cref="ColumnCount"/> may be 0 because the structure was not enumerated.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrTable
{
    /// <summary>Initializes a new instance of the <see cref="OcrTable"/> class.</summary>
    /// <param name="rowCount">The number of rows in the table.</param>
    /// <param name="columnCount">The number of columns in the table.</param>
    /// <param name="cells">The structured cells, or <see langword="null"/> when only markdown is available.</param>
    /// <param name="markdownRepresentation">The markdown or HTML representation, or <see langword="null"/> when cells are available.</param>
    public OcrTable(
        int rowCount,
        int columnCount,
        IReadOnlyList<OcrTableCell>? cells = null,
        string? markdownRepresentation = null)
    {
        RowCount = rowCount;
        ColumnCount = columnCount;
        Cells = cells;
        MarkdownRepresentation = markdownRepresentation;
    }

    /// <summary>Gets the number of rows in the table.</summary>
    public int RowCount { get; }

    /// <summary>Gets the number of columns in the table.</summary>
    public int ColumnCount { get; }

    /// <summary>Gets the structured cells, or <see langword="null"/> when the engine only returned markdown.</summary>
    public IReadOnlyList<OcrTableCell>? Cells { get; }

    /// <summary>Gets the markdown or HTML table text, or <see langword="null"/> when only cells were returned.</summary>
    public string? MarkdownRepresentation { get; }

    /// <summary>Gets or sets the region of the page the table occupies, when the engine provides geometry.</summary>
    public OcrBoundingRegion? BoundingRegion { get; set; }
}

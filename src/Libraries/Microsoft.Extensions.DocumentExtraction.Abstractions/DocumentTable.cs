// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Represents a table extracted from a document.</summary>
/// <remarks>
/// Cells are the primary, structured representation (row and column indices with spans, the Azure
/// Document Intelligence shape) and are authoritative when non-<see langword="null"/>.
/// <see cref="MarkdownRepresentation"/> is the fallback for engines that only emit markdown or HTML
/// (such as Mistral OCR). Consumers prefer <see cref="Cells"/> when present and fall back to
/// <see cref="MarkdownRepresentation"/> otherwise. On the markdown-only path <see cref="RowCount"/> and
/// <see cref="ColumnCount"/> may be 0 because the structure was not enumerated.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public class DocumentTable : DocumentElement
{
    /// <summary>Initializes a new instance of the <see cref="DocumentTable"/> class.</summary>
    /// <param name="rowCount">The number of rows in the table.</param>
    /// <param name="columnCount">The number of columns in the table.</param>
    /// <param name="cells">The structured cells, or <see langword="null"/> when only markdown is available.</param>
    /// <param name="markdownRepresentation">The markdown or HTML representation, or <see langword="null"/> when cells are available.</param>
    public DocumentTable(
        int rowCount,
        int columnCount,
        IReadOnlyList<DocumentTableCell>? cells = null,
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
    public IReadOnlyList<DocumentTableCell>? Cells { get; }

    /// <summary>Gets the markdown or HTML table text, or <see langword="null"/> when only cells were returned.</summary>
    public string? MarkdownRepresentation { get; }
}

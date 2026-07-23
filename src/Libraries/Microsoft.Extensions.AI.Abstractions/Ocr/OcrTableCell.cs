// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a single cell within an <see cref="OcrTable"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrTableCell
{
    /// <summary>Initializes a new instance of the <see cref="OcrTableCell"/> class.</summary>
    /// <param name="rowIndex">The zero-based row index of the cell.</param>
    /// <param name="columnIndex">The zero-based column index of the cell.</param>
    /// <param name="content">The text content of the cell.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    public OcrTableCell(int rowIndex, int columnIndex, string content)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Content = Throw.IfNull(content);
    }

    /// <summary>Gets or sets the role of the cell, for example <see cref="OcrTableCellKind.ColumnHeader"/> or <see cref="OcrTableCellKind.Content"/>.</summary>
    public OcrTableCellKind? Kind { get; set; }

    /// <summary>Gets the zero-based row index of the cell.</summary>
    public int RowIndex { get; }

    /// <summary>Gets the zero-based column index of the cell.</summary>
    public int ColumnIndex { get; }

    /// <summary>Gets or sets the number of rows the cell spans. The default is 1.</summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>Gets or sets the number of columns the cell spans. The default is 1.</summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>Gets the text content of the cell.</summary>
    public string Content { get; }

    /// <summary>Gets or sets the nested content of the cell, in reading order, when the engine provides structured cell content.</summary>
    /// <remarks>
    /// When <see langword="null"/>, the cell is text-only and <see cref="Content"/> carries its text. When present,
    /// the cell holds richer structured content (for example nested blocks or tables), and <see cref="Content"/>
    /// remains a flat-text convenience. This mirrors the nested-content model used by engines such as Docling and
    /// Google Document AI.
    /// </remarks>
    public IReadOnlyList<OcrElement>? Elements { get; set; }
}

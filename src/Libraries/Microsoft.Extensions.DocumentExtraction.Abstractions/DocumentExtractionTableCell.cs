// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Represents one addressed table cell.
/// </summary>
public sealed class DocumentExtractionTableCell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionTableCell"/> class.
    /// </summary>
    /// <param name="row">The zero-based row.</param>
    /// <param name="column">The zero-based column.</param>
    /// <param name="content">The cell observation.</param>
    /// <param name="rowSpan">The positive number of rows occupied.</param>
    /// <param name="columnSpan">The positive number of columns occupied.</param>
    /// <param name="kind">The optional native cell role.</param>
    /// <param name="annotations">Optional annotations independent of content.</param>
    public DocumentExtractionTableCell(
        int row,
        int column,
        DocumentExtractionObservation? content,
        int rowSpan = 1,
        int columnSpan = 1,
        string? kind = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null)
    {
        _ = Throw.IfLessThan(row, 0, nameof(row));
        _ = Throw.IfLessThan(column, 0, nameof(column));
        Row = row;
        Column = column;
        Content = content;
        RowSpan = Throw.IfLessThan(rowSpan, 1);
        ColumnSpan = Throw.IfLessThan(columnSpan, 1);
        Kind = kind is null ? null : Throw.IfNullOrWhitespace(kind);
        Annotations = DocumentExtractionObservation.Copy(annotations);
        foreach (DocumentAnnotation annotation in Annotations)
        {
            _ = Throw.IfNull(annotation);
        }
    }

    /// <summary>Gets the zero-based row.</summary>
    public int Row { get; }

    /// <summary>Gets the zero-based column.</summary>
    public int Column { get; }

    /// <summary>Gets the cell content observation.</summary>
    public DocumentExtractionObservation? Content { get; }

    /// <summary>Gets the positive number of rows covered by the cell.</summary>
    public int RowSpan { get; }

    /// <summary>Gets the positive number of columns covered by the cell.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the optional producer-supplied cell role.</summary>
    public string? Kind { get; }

    /// <summary>Gets source annotations, independently of content presence.</summary>
    public IReadOnlyList<DocumentAnnotation> Annotations { get; }
}

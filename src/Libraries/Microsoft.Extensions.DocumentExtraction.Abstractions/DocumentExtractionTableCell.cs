// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public DocumentExtractionTableCell(int row, int column, DocumentExtractionObservation content)
    {
        _ = Throw.IfLessThan(row, 0, nameof(row));
        _ = Throw.IfLessThan(column, 0, nameof(column));
        Row = row;
        Column = column;
        Content = Throw.IfNull(content);
    }

    /// <summary>Gets the zero-based row.</summary>
    public int Row { get; }

    /// <summary>Gets the zero-based column.</summary>
    public int Column { get; }

    /// <summary>Gets the cell content observation.</summary>
    public DocumentExtractionObservation Content { get; }
}

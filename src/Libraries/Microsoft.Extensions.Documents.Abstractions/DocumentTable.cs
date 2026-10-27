// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents a table in a document.
/// </summary>
public sealed class DocumentTableElement : DocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTableElement"/> class.
    /// </summary>
    /// <param name="markdown">The stored markdown representation of the table.</param>
    /// <param name="rowCount">The number of rows.</param>
    /// <param name="columnCount">The number of columns.</param>
    public DocumentTableElement(string markdown, int rowCount, int columnCount)
        : base(markdown)
    {
        _ = Throw.IfLessThan(rowCount, 0);
        _ = Throw.IfLessThan(columnCount, 0);

        Cells = new DocumentTableCellCollection(this, rowCount, columnCount);
    }

    /// <summary>
    /// Gets the addressable nullable cells in this table.
    /// </summary>
    public DocumentTableCellCollection Cells { get; }

    /// <inheritdoc/>
    public override string ToMarkdown()
    {
        if (Cells.RowCount == 0 || Cells.ColumnCount == 0)
        {
            return Markdown;
        }

        if (Cells.Any(static cell => cell is not null && (cell.RowSpan != 1 || cell.ColumnSpan != 1 || cell.Role is not null)))
        {
            List<string> htmlRows = new(Cells.RowCount);
            for (int row = 0; row < Cells.RowCount; row++)
            {
                List<string> htmlCells = [];
                for (int column = 0; column < Cells.ColumnCount; column++)
                {
                    if (Cells.IsCovered(row, column))
                    {
                        continue;
                    }

                    DocumentTableCell? cell = Cells[row, column];
                    string tag = cell?.Role is "columnHeader" or "rowHeader" ? "th" : "td";
                    string attributes = cell is null ? string.Empty :
                        $" rowspan=\"{cell.RowSpan}\" colspan=\"{cell.ColumnSpan}\"" +
                        (cell.Role is null ? string.Empty : $" data-kind=\"{WebUtility.HtmlEncode(cell.Role)}\"");
                    htmlCells.Add($"<{tag}{attributes}>{WebUtility.HtmlEncode(cell?.Content?.ToMarkdown() ?? string.Empty)}</{tag}>");
                }

                htmlRows.Add($"<tr>{string.Concat(htmlCells)}</tr>");
            }

            return $"<table>{string.Concat(htmlRows)}</table>";
        }

        List<string> rows = new(Cells.RowCount);
        for (int row = 0; row < Cells.RowCount; row++)
        {
            List<string> columns = new(Cells.ColumnCount);
            for (int column = 0; column < Cells.ColumnCount; column++)
            {
                DocumentTableCell? cell = Cells[row, column];
                string value = cell?.Content?.ToMarkdown() ?? string.Empty;
                columns.Add(value.Replace("|", "\\|"));
            }

            rows.Add($"| {string.Join(" | ", columns)} |");
        }

        return string.Join(Environment.NewLine, rows);
    }
}

/// <summary>
/// Represents one addressable table cell.
/// </summary>
public sealed class DocumentTableCell
{
    private DocumentTableElement? _table;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTableCell"/> class.
    /// </summary>
    /// <param name="content">The element in the cell.</param>
    /// <param name="rowSpan">The positive number of rows covered by the cell.</param>
    /// <param name="columnSpan">The positive number of columns covered by the cell.</param>
    /// <param name="role">The optional producer-supplied cell role.</param>
    public DocumentTableCell(DocumentElement content, int rowSpan = 1, int columnSpan = 1, string? role = null)
    {
        Content = Throw.IfNull(content);
        RowSpan = Throw.IfLessThan(rowSpan, 1);
        ColumnSpan = Throw.IfLessThan(columnSpan, 1);
        Role = role is null ? null : Throw.IfNullOrWhitespace(role);
        Sources = new DocumentSourceReferenceCollection(() => _table?.OwningDocument);
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentTableCell"/> class without content.</summary>
    public DocumentTableCell(int rowSpan = 1, int columnSpan = 1, string? role = null)
    {
        RowSpan = Throw.IfLessThan(rowSpan, 1);
        ColumnSpan = Throw.IfLessThan(columnSpan, 1);
        Role = role is null ? null : Throw.IfNullOrWhitespace(role);
        Sources = new DocumentSourceReferenceCollection(() => _table?.OwningDocument);
    }

    /// <summary>
    /// Gets the element in the cell.
    /// </summary>
    public DocumentElement? Content { get; }

    /// <summary>Gets the number of rows occupied by this anchor cell.</summary>
    public int RowSpan { get; }

    /// <summary>Gets the number of columns occupied by this anchor cell.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the optional producer-supplied cell role.</summary>
    public string? Role { get; }

    /// <summary>Gets page and region associations independent of the presence of cell content.</summary>
    public IList<DocumentSourceReference> Sources { get; }

    /// <summary>
    /// Gets the row index of the cell.
    /// </summary>
    public int RowIndex { get; internal set; }

    /// <summary>
    /// Gets the column index of the cell.
    /// </summary>
    public int ColumnIndex { get; internal set; }

    /// <summary>
    /// Gets the local structural address of the cell.
    /// </summary>
    internal string? Address => _table?.OwningDocument?.GetAddress(_table) is string tableAddress
        ? $"{tableAddress}/c{RowIndex},{ColumnIndex}"
        : null;

    internal DocumentTableElement? Table => _table;

    internal void Attach(DocumentTableElement table, int row, int column)
    {
        if (_table is not null)
        {
            throw new InvalidOperationException("The table cell is already owned by a table.");
        }

        _table = table;
        RowIndex = row;
        ColumnIndex = column;
    }

    internal void Detach(DocumentTableElement table)
    {
        if (!ReferenceEquals(_table, table))
        {
            throw new InvalidOperationException("The table cell is not owned by the specified table.");
        }

        _table = null;
    }
}

/// <summary>
/// Provides explicit row and column addressing for nullable table cells.
/// </summary>
public sealed class DocumentTableCellCollection : IEnumerable<DocumentTableCell?>
{
    private readonly DocumentTableElement _owner;
    private readonly DocumentTableCell?[,] _cells;
    private readonly DocumentTableCell?[,] _coveringCells;

    internal DocumentTableCellCollection(DocumentTableElement owner, int rowCount, int columnCount)
    {
        _owner = owner;
        _cells = new DocumentTableCell?[rowCount, columnCount];
        _coveringCells = new DocumentTableCell?[rowCount, columnCount];
    }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int RowCount => _cells.GetLength(0);

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int ColumnCount => _cells.GetLength(1);

    /// <summary>
    /// Gets or sets a cell by row and column address. <see langword="null"/> represents an empty cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    public DocumentTableCell? this[int row, int column]
    {
        get
        {
            ValidateAddress(row, column);
            return _cells[row, column];
        }
        set
        {
            ValidateAddress(row, column);
            _owner.OwningDocument?.ThrowIfDisposed();

            if (IsCovered(row, column))
            {
                throw new InvalidOperationException("A covered position must be edited through its anchor cell.");
            }

            DocumentTableCell? oldCell = _cells[row, column];
            if (ReferenceEquals(oldCell, value))
            {
                return;
            }

            if (value is not null)
            {
                ValidateSpan(row, column, value, oldCell);
                ValidateCanAdd(value);
                if (value.Content is { } candidateContent)
                {
                    _owner.OwningDocument?.ValidateOwnership(candidateContent);
                }

                _owner.OwningDocument?.ValidateSources(value.Sources.Concat(
                    value.Content is { } content ? Document.EnumerateSources(content) : Enumerable.Empty<DocumentSourceReference>()));
                value.Attach(_owner, row, column);
                try
                {
                    foreach (DocumentSourceReference source in value.Sources)
                    {
                        _owner.OwningDocument?.RegisterSource(source);
                    }

                    value.Content?.Attach(_owner, _owner.OwningDocument);
                }
                catch
                {
                    value.Detach(_owner);
                    throw;
                }
            }

            if (oldCell is not null)
            {
                SetCoverage(oldCell, null);
                oldCell.Content?.Detach(_owner);
                oldCell.Detach(_owner);
            }

            _cells[row, column] = value;
            if (value is not null)
            {
                SetCoverage(value, value);
            }

            _owner.NotifyMutation();
        }
    }

    /// <summary>Gets the anchor cell covering a position, or <see langword="null"/> for an empty position.</summary>
    /// <returns>The covering anchor, or <see langword="null"/>.</returns>
    internal DocumentTableCell? GetCoveringCell(int row, int column)
    {
        ValidateAddress(row, column);
        return _coveringCells[row, column];
    }

    /// <summary>Gets whether a position is covered by an anchor at a different address.</summary>
    /// <returns>Whether another anchor covers the position.</returns>
    internal bool IsCovered(int row, int column)
        => GetCoveringCell(row, column) is { } cell && (cell.RowIndex != row || cell.ColumnIndex != column);

    /// <summary>
    /// Gets the cell containing an element.
    /// </summary>
    internal DocumentTableCell GetCell(DocumentElement element)
    {
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0; column < ColumnCount; column++)
            {
                DocumentTableCell? cell = _cells[row, column];
                if (cell is not null && ReferenceEquals(cell.Content, element))
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException("The element is not a cell in this table.");
    }

    /// <inheritdoc/>
    public IEnumerator<DocumentTableCell?> GetEnumerator()
    {
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0; column < ColumnCount; column++)
            {
                yield return _cells[row, column];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void ValidateCanAdd(DocumentTableCell cell)
    {
        if (cell.Table is not null)
        {
            throw new InvalidOperationException("The table cell is already owned by a table.");
        }

        if (cell.Content is DocumentSection || ReferenceEquals(cell.Content, _owner) || cell.Content?.Contains(_owner) == true)
        {
            throw new InvalidOperationException("Adding the cell content would create an invalid table relationship.");
        }

        if (cell.Content?.Parent is not null)
        {
            throw new InvalidOperationException("The cell content is already owned by a document hierarchy.");
        }
    }

    private void ValidateSpan(int row, int column, DocumentTableCell cell, DocumentTableCell? replaced)
    {
        if (cell.RowSpan > RowCount - row || cell.ColumnSpan > ColumnCount - column)
        {
            Throw.ArgumentException(nameof(cell), "A cell span must fit within the table.");
        }

        for (int r = row; r < row + cell.RowSpan; r++)
        {
            for (int c = column; c < column + cell.ColumnSpan; c++)
            {
                if (GetCoveringCell(r, c) is { } existing && !ReferenceEquals(existing, replaced))
                {
                    throw new InvalidOperationException("Cell spans cannot overlap another cell.");
                }
            }
        }
    }

    private void ValidateAddress(int row, int column)
    {
        _ = Throw.IfLessThan(row, 0, nameof(row));
        _ = Throw.IfGreaterThanOrEqual(row, RowCount, nameof(row));
        _ = Throw.IfLessThan(column, 0, nameof(column));
        _ = Throw.IfGreaterThanOrEqual(column, ColumnCount, nameof(column));
    }

    private void SetCoverage(DocumentTableCell cell, DocumentTableCell? covering)
    {
        for (int row = cell.RowIndex; row < cell.RowIndex + cell.RowSpan; row++)
        {
            for (int column = cell.ColumnIndex; column < cell.ColumnIndex + cell.ColumnSpan; column++)
            {
                _coveringCells[row, column] = covering;
            }
        }
    }
}

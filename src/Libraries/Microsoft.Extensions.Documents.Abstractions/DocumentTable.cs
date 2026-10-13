// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
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
            return StoredMarkdown;
        }

        List<string> rows = new(Cells.RowCount);
        for (int row = 0; row < Cells.RowCount; row++)
        {
            List<string> columns = new(Cells.ColumnCount);
            for (int column = 0; column < Cells.ColumnCount; column++)
            {
                DocumentTableCell? cell = Cells[row, column];
                string value = cell?.Content.ToMarkdown() ?? string.Empty;
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
    public DocumentTableCell(DocumentElement content)
    {
        Content = Throw.IfNull(content);
    }

    /// <summary>
    /// Gets the element in the cell.
    /// </summary>
    public DocumentElement Content { get; }

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
    public string? Address => _table?.OwningDocument?.GetAddress(_table) is string tableAddress
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

    internal DocumentTableCellCollection(DocumentTableElement owner, int rowCount, int columnCount)
    {
        _owner = owner;
        _cells = new DocumentTableCell?[rowCount, columnCount];
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

            DocumentTableCell? oldCell = _cells[row, column];
            if (ReferenceEquals(oldCell, value))
            {
                return;
            }

            if (value is not null)
            {
                ValidateCanAdd(value);
                value.Attach(_owner, row, column);
                try
                {
                    value.Content.Attach(_owner, _owner.OwningDocument);
                }
                catch
                {
                    value.Detach(_owner);
                    throw;
                }
            }

            if (oldCell is not null)
            {
                oldCell.Content.Detach(_owner);
                oldCell.Detach(_owner);
            }

            _cells[row, column] = value;
            _owner.NotifyMutation();
        }
    }

    /// <summary>
    /// Sets a cell's content, or clears the cell when <paramref name="content"/> is <see langword="null"/>.
    /// </summary>
    public void SetContent(int row, int column, DocumentElement? content)
        => this[row, column] = content is null ? null : new DocumentTableCell(content);

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

        if (cell.Content is DocumentSection || ReferenceEquals(cell.Content, _owner) || cell.Content.Contains(_owner))
        {
            throw new InvalidOperationException("Adding the cell content would create an invalid table relationship.");
        }

        if (cell.Content.Parent is not null)
        {
            throw new InvalidOperationException("The cell content is already owned by a document hierarchy.");
        }
    }

    private void ValidateAddress(int row, int column)
    {
        _ = Throw.IfLessThan(row, 0, nameof(row));
        _ = Throw.IfGreaterThanOrEqual(row, RowCount, nameof(row));
        _ = Throw.IfLessThan(column, 0, nameof(column));
        _ = Throw.IfGreaterThanOrEqual(column, ColumnCount, nameof(column));
    }
}

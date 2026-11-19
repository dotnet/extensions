// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>Provides extension methods for reading text from structured documents.</summary>
public static class ExtractionDocumentExtensions
{
    /// <summary>Gets all text in a document, in document order.</summary>
    /// <remarks>
    /// <para>
    /// The text is read depth-first in pre-order: a node's local <see cref="TextContent"/> payload is read before its
    /// children. The document's local payload is read first, followed by its sections. Each section's local payload
    /// is followed by its elements. Each table's local payload is followed by the content of each anchor cell in
    /// row-major order. Each image's local <see cref="TextContent"/> payload is followed by
    /// <see cref="ExtractionDocumentImageElement.Text"/>.
    /// </para>
    /// <para>
    /// Only <see cref="TextContent"/> payloads and image text are read. Other payloads such as bytes or references,
    /// <see cref="ExtractionDocumentImageElement.AlternativeText"/>, metadata and native representations are excluded.
    /// Elements are not filtered by role, so page headers and footers are included. Text is returned verbatim, without
    /// converting formatted text, and non-empty text blocks are separated by <see cref="Environment.NewLine"/>.
    /// </para>
    /// <para>
    /// Shared acyclic content is read at every occurrence. Cyclic section or table relationships and null entries are
    /// rejected.
    /// </para>
    /// </remarks>
    /// <param name="document">The document to read.</param>
    /// <returns>The text in the document, or an empty string when the document has no text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The document contains a null entry or a cyclic relationship.</exception>
    public static string GetText(this ExtractionDocument document)
    {
        _ = Throw.IfNull(document);

        StringBuilder builder = new();
        HashSet<ExtractionDocumentElement> path = [];
        AppendPayload(builder, document.Content);
        foreach (ExtractionDocumentSection section in document.Sections)
        {
            if (section is null)
            {
                throw new InvalidOperationException("A document cannot traverse a null section.");
            }

            AppendElement(builder, section, path);
        }

        return builder.ToString();
    }

    /// <summary>Gets all text in an element and its descendants, in document order.</summary>
    /// <remarks>
    /// This method reads text with the same rules as <see cref="GetText(ExtractionDocument)"/>. For a custom element
    /// type, only its local <see cref="TextContent"/> payload is read.
    /// </remarks>
    /// <param name="element">The element to read.</param>
    /// <returns>The text in the element, or an empty string when the element has no text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The element contains a null entry or a cyclic relationship.</exception>
    public static string GetText(this ExtractionDocumentElement element)
    {
        _ = Throw.IfNull(element);

        StringBuilder builder = new();
        AppendElement(builder, element, []);
        return builder.ToString();
    }

    /// <summary>Gets all text in a table cell's content, in document order.</summary>
    /// <remarks>This method reads text with the same rules as <see cref="GetText(ExtractionDocument)"/>.</remarks>
    /// <param name="cell">The table cell to read.</param>
    /// <returns>The text in the cell, or an empty string when the cell has no text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cell"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The cell contains a null entry or a cyclic relationship.</exception>
    public static string GetText(this ExtractionDocumentTableCell cell)
    {
        _ = Throw.IfNull(cell);

        StringBuilder builder = new();
        if (cell.Content is { } content)
        {
            AppendElement(builder, content, []);
        }

        return builder.ToString();
    }

    private static void AppendElement(StringBuilder builder, ExtractionDocumentElement element, HashSet<ExtractionDocumentElement> path)
    {
        AppendPayload(builder, element.Content);

        switch (element)
        {
            case ExtractionDocumentSection section:
                Enter(section, path);
                try
                {
                    foreach (ExtractionDocumentElement child in section.Elements)
                    {
                        if (child is null)
                        {
                            throw new InvalidOperationException("A document cannot traverse a null element.");
                        }

                        AppendElement(builder, child, path);
                    }
                }
                finally
                {
                    _ = path.Remove(section);
                }

                break;

            case ExtractionDocumentTableElement table:
                Enter(table, path);
                try
                {
                    foreach (ExtractionDocumentTableCell cell in table.Cells)
                    {
                        if (cell.Content is { } content)
                        {
                            AppendElement(builder, content, path);
                        }
                    }
                }
                finally
                {
                    _ = path.Remove(table);
                }

                break;

            case ExtractionDocumentImageElement image:
                AppendText(builder, image.Text);
                break;
        }
    }

    private static void Enter(ExtractionDocumentElement container, HashSet<ExtractionDocumentElement> path)
    {
        if (!path.Add(container))
        {
            throw new InvalidOperationException("A document cannot traverse a cyclic section or table relationship.");
        }
    }

    private static void AppendPayload(StringBuilder builder, AIContent? content)
    {
        if (content is TextContent text)
        {
            AppendText(builder, text.Text);
        }
    }

    private static void AppendText(StringBuilder builder, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (builder.Length > 0)
        {
            _ = builder.Append(Environment.NewLine);
        }

        _ = builder.Append(text);
    }
}

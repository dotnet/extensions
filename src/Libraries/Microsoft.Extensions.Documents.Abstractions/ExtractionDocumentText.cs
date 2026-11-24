// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.Documents;

/// <summary>Reads plain text from structured documents for the <c>GetText</c> methods.</summary>
internal static class ExtractionDocumentText
{
    public static string Read(ExtractionDocument document)
    {
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

    public static string Read(ExtractionDocumentElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        AppendElement(builder, element, []);
        return builder.ToString();
    }

    private static void AppendElement(StringBuilder builder, ExtractionDocumentElement element, HashSet<ExtractionDocumentElement> path)
    {
        AppendPayload(builder, element.Content);
        AppendText(builder, element.ReadAdditionalText());

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

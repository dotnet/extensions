// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

public sealed class MarkdownReader : IngestionDocumentReader
{
    public override async Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }

#if NET
        string fileContent = await File.ReadAllTextAsync(source.FullName, cancellationToken);
#else
        using FileStream stream = new(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, FileOptions.Asynchronous);
        string fileContent = await ReadToEndAsync(stream, cancellationToken);
#endif
        return Read(fileContent, identifier);
    }

    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        string fileContent = await ReadToEndAsync(source, cancellationToken);
        return Read(fileContent, identifier);
    }

    public IngestionDocument Read(string fileContent, string identifier)
    {
        if (string.IsNullOrEmpty(fileContent))
        {
            throw new ArgumentNullException(nameof(fileContent));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        // Markdig's "UseAdvancedExtensions" option includes many common extensions beyond
        // CommonMark, such as citations, figures, footnotes, grid tables, mathematics
        // task lists, diagrams, and more.
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        MarkdownDocument markdownDocument = Markdown.Parse(fileContent, pipeline);
        return Map(markdownDocument, fileContent, identifier);
    }

    private static async Task<string> ReadToEndAsync(Stream source, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(source, encoding: null, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: true);
        return await reader.ReadToEndAsync(
#if NET
            cancellationToken
#endif
        );
    }

    private static IngestionDocument Map(MarkdownDocument markdownDocument, string outputContent, string identifier)
    {
        IngestionDocumentSection rootSection = new(outputContent);
        IngestionDocument result = new(identifier)
        {
            Sections = { rootSection }
        };

        bool previousWasBreak = false;
        foreach (Block block in markdownDocument)
        {
            if (block is ThematicBreakBlock breakBlock)
            {
                // We have encountered a thematic break (horizontal rule): ----------- etc.
                previousWasBreak = true;
                continue;
            }

            if (block is LinkReferenceDefinitionGroup linkReferenceGroup)
            {
                continue; // In the future, we might want to handle links differently.
            }

            if (IsEmptyBlock(block))
            {
                continue;
            }

            rootSection.Elements.Add(MapBlock(outputContent, previousWasBreak, block));
            previousWasBreak = false;
        }

        return result;
    }

    private static bool IsEmptyBlock(Block block) // Block with no text. Sample: QuoteBlock the next block is a quote.
        => block is LeafBlock emptyLeafBlock && (emptyLeafBlock.Inline is null || emptyLeafBlock.Inline.FirstChild is null);

    private static IngestionDocumentElement MapBlock(string outputContent, bool previousWasBreak, Block block)
    {
        string elementMarkdown = outputContent.Substring(block.Span.Start, block.Span.Length);

        IngestionDocumentElement element = block switch
        {
            LeafBlock leafBlock => MapLeafBlockToElement(leafBlock, previousWasBreak, elementMarkdown),
            ListBlock listBlock => MapListBlock(listBlock, previousWasBreak, outputContent, elementMarkdown),
            QuoteBlock quoteBlock => MapQuoteBlock(quoteBlock, previousWasBreak, outputContent, elementMarkdown),
            Table table => new IngestionDocumentTable(elementMarkdown, GetCells(table, outputContent)),
            _ => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported.")
        };

        return element;
    }

    private static IngestionDocumentElement MapLeafBlockToElement(LeafBlock block, bool previousWasBreak, string elementMarkdown)
        => block switch
        {
            HeadingBlock heading => new IngestionDocumentHeader(elementMarkdown)
            {
                Text = GetText(heading.Inline),
                Level = heading.Level
            },
            ParagraphBlock footer when previousWasBreak => new IngestionDocumentFooter(elementMarkdown)
            {
                Text = GetText(footer.Inline),
            },
            ParagraphBlock image when image.Inline!.Descendants<LinkInline>().FirstOrDefault() is LinkInline link && link.IsImage => new IngestionDocumentImage(elementMarkdown)
            {
                // ![Alt text](data:image/png;base64,...)
                AlternativeText = link.FirstChild is LiteralInline literal ? literal.Content.ToString() : null,
                Content = link.Url is not null && link.Url.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                    ? Convert.FromBase64String(link.Url.Substring("data:image/png;base64,".Length))
                    : null, // we may implement it in the future if needed
                MediaType = link.Url is not null && link.Url.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                    ? "image/png"
                    : null // we may implement it in the future if needed
            },
            ParagraphBlock paragraph => new IngestionDocumentParagraph(elementMarkdown)
            {
                Text = GetText(paragraph.Inline),
            },
            CodeBlock codeBlock => new IngestionDocumentParagraph(elementMarkdown)
            {
                Text = GetText(codeBlock.Inline),
            },
            _ => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported.")
        };

    private static IngestionDocumentSection MapListBlock(ListBlock listBlock, bool previousWasBreak, string outputContent, string listMarkdown)
    {
        // So far Sections were only pages (LP) or sections for ADI. Now they can also represent lists.
        IngestionDocumentSection list = new(listMarkdown);
        foreach (Block? item in listBlock)
        {
            if (item is not ListItemBlock listItemBlock)
            {
                continue;
            }

            foreach (Block? child in listItemBlock)
            {
                if (child is not LeafBlock leafBlock || IsEmptyBlock(leafBlock))
                {
                    continue; // Skip empty blocks in lists
                }

                string childMarkdown = outputContent.Substring(leafBlock.Span.Start, leafBlock.Span.Length);
                IngestionDocumentElement element = MapLeafBlockToElement(leafBlock, previousWasBreak, childMarkdown);
                list.Elements.Add(element);
            }
        }

        return list;
    }

    private static IngestionDocumentSection MapQuoteBlock(QuoteBlock quoteBlock, bool previousWasBreak, string outputContent, string elementMarkdown)
    {
        // So far Sections were only pages (LP) or sections for ADI. Now they can also represent quotes.
        IngestionDocumentSection quote = new(elementMarkdown);
        foreach (Block child in quoteBlock)
        {
            if (IsEmptyBlock(child))
            {
                continue; // Skip empty blocks in quotes
            }

            quote.Elements.Add(MapBlock(outputContent, previousWasBreak, child));
        }

        return quote;
    }

    private static string? GetText(ContainerInline? containerInline)
    {
        Debug.Assert(containerInline != null, "ContainerInline should not be null here.");
        Debug.Assert(containerInline.FirstChild != null, "FirstChild should not be null here.");

        if (ReferenceEquals(containerInline.FirstChild, containerInline.LastChild))
        {
            // If there is only one child, return its text.
            return containerInline.FirstChild.ToString();
        }

        StringBuilder content = new(100);
        foreach (Inline inline in containerInline)
        {
            if (inline is LiteralInline literalInline)
            {
                content.Append(literalInline.Content);
            }
            else if (inline is LineBreakInline)
            {
                content.AppendLine(); // Append a new line for line breaks
            }
            else if (inline is ContainerInline another)
            {
                // EmphasisInline is also a ContainerInline, but it does not get any special treatment,
                // as we use raw text here (instead of a markdown, where emphasis can be expressed).
                content.Append(GetText(another));
            }
            else if (inline is CodeInline codeInline)
            {
                content.Append(codeInline.Content);
            }
            else
            {
                throw new NotSupportedException($"Inline type '{inline.GetType().Name}' is not supported.");
            }
        }

        return content.ToString();
    }

    private static IngestionDocumentElement?[,] GetCells(Table table, string outputContent)
    {
        int firstRowIndex = SkipFirstRow(table, outputContent) ? 1 : 0;
        var cells = new IngestionDocumentElement?[table.Count - firstRowIndex, table.ColumnDefinitions.Count - 1];

        for (int rowIndex = firstRowIndex; rowIndex < table.Count; rowIndex++)
        {
            TableRow tableRow = (TableRow)table[rowIndex];
            int columnIndex = 0;
            for (int cellIndex = 0; cellIndex < tableRow.Count; cellIndex++)
            {
                TableCell tableCell = (TableCell)tableRow[cellIndex];
                var content = tableCell.Count switch
                {
                    0 => null,
                    1 => MapBlock(outputContent, previousWasBreak: false, tableCell[0]),
                    _ => throw new NotSupportedException($"Cells with {tableCell.Count} elements are not supported.")
                };

                for (int columnSpan = 0; columnSpan < tableCell.ColumnSpan; columnSpan++, columnIndex++)
                {
                    // We are not using tableCell.ColumnIndex here as it defaults to -1 ;)
                    cells[rowIndex - firstRowIndex, columnIndex] = content;
                }
            }
        }

        return cells;

        // Some parsers like MarkItDown include a row with invalid markdown before the separator row:
        // |  |  |  |  |
        // | --- | --- | --- | --- |
        static bool SkipFirstRow(Table table, string outputContent)
        {
            if (table.Count > 0)
            {
                TableRow firstRow = (TableRow)table[0];
                for (int cellIndex = 0; cellIndex < firstRow.Count; cellIndex++)
                {
                    TableCell tableCell = (TableCell)firstRow[cellIndex];
                    if (!string.IsNullOrWhiteSpace(MapBlock(outputContent, previousWasBreak: false, tableCell[0]).Text))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingContext
    {
        public Uri Uri { get; set; }

        public RazorCodeDocument CodeDocument { get; set; }

        public SourceText SourceText => CodeDocument?.GetSourceText();

        public FormattingOptions Options { get; set; }

        public Range Range { get; set; }

        public Dictionary<int, IndentationContext> Indentations { get; } = new Dictionary<int, IndentationContext>();

        /// <summary>
        /// Generates a string of indentation based on a specific indentation level. For instance, inside of a C# method represents 1 indentation level. A method within a class would have indentaiton level of 2 by default etc.
        /// </summary>
        /// <param name="indentationLevel">The indentation level to represent</param>
        /// <returns>A whitespace string representing the indentation level based on the configuration.</returns>
        public string GetIndentationLevelString(int indentationLevel)
        {
            var indentation = indentationLevel * (int)Options.TabSize;
            var indentationString = GetIndentationString(indentation);
            return indentationString;
        }

        /// <summary>
        /// Given a <paramref name="indentation"/> amount of characters, generate a string representing the configured indentation.
        /// </summary>
        /// <param name="indentation">An amount of characters to represent the indentation</param>
        /// <returns>A whitespace string representation indentation.</returns>
        public string GetIndentationString(int indentation)
        {
            if (Options.InsertSpaces)
            {
                return new string(' ', indentation);
            }
            else
            {
                var tabs = indentation / Options.TabSize;
                var tabPrefix = new string('\t', (int)tabs);

                var spaces = indentation % Options.TabSize;
                var spaceSuffix = new string(' ', (int)spaces);

                var combined = string.Concat(tabPrefix, spaceSuffix);
                return combined;
            }
        }

        public static FormattingContext Create(Uri uri, RazorCodeDocument codedocument, Range range, FormattingOptions options)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (codedocument is null)
            {
                throw new ArgumentNullException(nameof(codedocument));
            }

            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var result = new FormattingContext()
            {
                Uri = uri,
                CodeDocument = codedocument,
                Range = range,
                Options = options
            };

            var source = codedocument.Source;
            var syntaxTree = codedocument.GetSyntaxTree();
            var formattingSpans = syntaxTree.GetFormattingSpans();

            var total = 0;
            var previousIndentationLevel = 0;
            for (var i = 0; i < source.Lines.Count; i++)
            {
                // Get first non-whitespace character position
                var lineLength = source.Lines.GetLineLength(i);
                var nonWsChar = 0;
                for (var j = 0; j < lineLength; j++)
                {
                    var ch = source[total + j];
                    if (!char.IsWhiteSpace(ch) && !ParserHelpers.IsNewLine(ch))
                    {
                        nonWsChar = j;
                        break;
                    }
                }

                // position now contains the first non-whitespace character or 0. Get the corresponding FormattingSpan.
                if (TryGetFormattingSpan(total + nonWsChar, formattingSpans, out var span))
                {
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = span.IndentationLevel,
                        RelativeIndentationLevel = span.IndentationLevel - previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                        FirstSpan = span,
                    };
                    previousIndentationLevel = span.IndentationLevel;
                }
                else
                {
                    // Couldn't find a corresponding FormattingSpan.
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = -1,
                        RelativeIndentationLevel = previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                    };
                }

                total += lineLength;
            }

            return result;
        }

        private static bool TryGetFormattingSpan(int absoluteIndex, IReadOnlyList<FormattingSpan> formattingspans, out FormattingSpan result)
        {
            result = null;
            for (var i = 0; i < formattingspans.Count; i++)
            {
                var formattingspan = formattingspans[i];
                var span = formattingspan.Span;

                if (span.Start <= absoluteIndex)
                {
                    if (span.End >= absoluteIndex)
                    {
                        if (span.End == absoluteIndex && span.Length > 0)
                        {
                            // We're at an edge.
                            // Non-marker spans (spans.length == 0) do not own the edges after it
                            continue;
                        }

                        result = formattingspan;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

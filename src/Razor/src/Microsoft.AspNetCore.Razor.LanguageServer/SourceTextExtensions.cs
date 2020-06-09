// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class SourceTextExtensions
    {
        /// <summary>
        /// Gets the minimal range of text that changed between the two versions.
        /// </summary>
        public static TextChangeRange GetEncompassingTextChangeRange(this SourceText newText, SourceText oldText)
        {
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            var ranges = newText.GetChangeRanges(oldText);
            if (ranges.Count == 0)
            {
                return default;
            }

            // simple case.
            if (ranges.Count == 1)
            {
                return ranges[0];
            }

            return TextChangeRange.Collapse(ranges);
        }

        public static void GetLineAndOffset(this SourceText source, int position, out int lineNumber, out int offset)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var line = source.Lines.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            offset = position - line.Start;
        }

        public static void GetLinesAndOffsets(
            this SourceText source,
            TextSpan textSpan,
            out int startLineNumber,
            out int startOffset,
            out int endLineNumber,
            out int endOffset)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            source.GetLineAndOffset(textSpan.Start, out startLineNumber, out startOffset);
            source.GetLineAndOffset(textSpan.End, out endLineNumber, out endOffset);
        }

        public static string GetSubTextString(this SourceText source, TextSpan span)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var charBuffer = new char[span.Length];
            source.CopyTo(span.Start, charBuffer, 0, span.Length);
            return new string(charBuffer);
        }
    }
}

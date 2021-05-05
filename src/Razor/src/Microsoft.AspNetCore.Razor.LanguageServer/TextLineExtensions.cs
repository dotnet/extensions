// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class TextLineExtensions
    {
        public static string GetLeadingWhitespace(this TextLine line)
        {
            return line.ToString().GetLeadingWhitespace();
        }

        public static int? GetFirstNonWhitespacePosition(this TextLine line)
        {
            var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset();

            return firstNonWhitespaceOffset.HasValue
                ? firstNonWhitespaceOffset + line.Start
                : null;
        }

        public static int? GetFirstNonWhitespaceOffset(this TextLine line, int startOffset = 0)
        {
            if (startOffset > line.SpanIncludingLineBreak.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startOffset), "Invalid offset.");
            }

            return line.Text.GetFirstNonWhitespaceOffset(TextSpan.FromBounds(line.Start + startOffset, line.EndIncludingLineBreak), out _);
        }
    }
}

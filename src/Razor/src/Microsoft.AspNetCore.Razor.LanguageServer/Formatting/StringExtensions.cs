// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal static class StringExtensions
    {
        public static int? GetFirstNonWhitespaceOffset(this string line)
        {
            if (line is null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            for (var i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return i;
                }
            }

            return null;
        }

        public static int? GetLastNonWhitespaceOffset(this string line)
        {
            if (line is null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            for (var i = line.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return i;
                }
            }

            return null;
        }

        public static string GetLeadingWhitespace(this string lineText)
        {
            if (lineText is null)
            {
                throw new ArgumentNullException(nameof(lineText));
            }

            var firstOffset = lineText.GetFirstNonWhitespaceOffset();

            return firstOffset.HasValue
                ? lineText.Substring(0, firstOffset.Value)
                : lineText;
        }

        public static string GetTrailingWhitespace(this string lineText)
        {
            if (lineText is null)
            {
                throw new ArgumentNullException(nameof(lineText));
            }

            var lastOffset = lineText.GetLastNonWhitespaceOffset();

            return lastOffset.HasValue
                ? lineText.Substring(lastOffset.Value)
                : lineText;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Gen.Logging.Parsing;

internal static class TemplateExtractor
{
    private static readonly char[] _formatDelimiters = { ',', ':' };

    /// <summary>
    /// Finds the template arguments contained in the message string.
    /// </summary>
    internal static void ExtractTemplates(string? message, IDictionary<string, string> templateToParameterName, out ICollection<string> templatesWithAtSymbol)
    {
        if (string.IsNullOrEmpty(message))
        {
            templatesWithAtSymbol = Array.Empty<string>();
            return;
        }

        var scanIndex = 0;
        var endIndex = message!.Length;
        ICollection<string> foundAtTemplates = null!;
        while (scanIndex < endIndex)
        {
            var openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);
            var closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex, endIndex);

            if (closeBraceIndex == endIndex)
            {
                scanIndex = endIndex;
            }
            else
            {
                // Format item syntax : { index[,alignment][ :formatString] }.
                var formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);

                var templateName = message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1).Trim();
                if (templateName[0] == '@')
                {
                    foundAtTemplates ??= new List<string>();
                    foundAtTemplates.Add(templateName);
                    templateName = templateName.Substring(1);
                }

                templateToParameterName[templateName] = templateName;
                scanIndex = closeBraceIndex + 1;
            }
        }

        templatesWithAtSymbol = foundAtTemplates ?? Array.Empty<string>();
    }

    internal static int FindIndexOfAny(string message, char[] chars, int startIndex, int endIndex)
    {
        var findIndex = message.IndexOfAny(chars, startIndex, endIndex - startIndex);
        return findIndex == -1
            ? endIndex
            : findIndex;
    }

    private static int FindBraceIndex(string message, char brace, int startIndex, int endIndex)
    {
        // Example: {{prefix{{{Argument}}}suffix}}.
        var braceIndex = endIndex;
        var scanIndex = startIndex;
        var braceOccurrenceCount = 0;

        while (scanIndex < endIndex)
        {
            if (braceOccurrenceCount > 0 && message[scanIndex] != brace)
            {
#pragma warning disable S109 // Magic numbers should not be used
                if (braceOccurrenceCount % 2 == 0)
#pragma warning restore S109 // Magic numbers should not be used
                {
                    // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                    braceOccurrenceCount = 0;
                    braceIndex = endIndex;
                }
                else
                {
                    // An unescaped '{' or '}' found.
                    break;
                }
            }
            else if (message[scanIndex] == brace)
            {
                if (brace == '}')
                {
                    if (braceOccurrenceCount == 0)
                    {
                        // For '}' pick the first occurrence.
                        braceIndex = scanIndex;
                    }
                }
                else
                {
                    // For '{' pick the last occurrence.
                    braceIndex = scanIndex;
                }

                braceOccurrenceCount++;
            }

            scanIndex++;
        }

        return braceIndex;
    }
}

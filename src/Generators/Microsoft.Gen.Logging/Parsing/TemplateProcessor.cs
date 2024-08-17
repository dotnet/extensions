// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Gen.Logging.Parsing;

internal static class TemplateProcessor
{
    private static readonly char[] _formatDelimiters = { ',', ':' };

    /// <summary>
    /// Finds the template arguments contained in the message string.
    /// </summary>
    internal static bool ExtractTemplates(string? message, List<string> templates)
    {
        if (string.IsNullOrEmpty(message))
        {
            return true;
        }

        var scanIndex = 0;
        var endIndex = message!.Length;

        bool success = true;
        while (scanIndex < endIndex)
        {
            var openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);

#pragma warning disable S109 // Magic numbers should not be used
            if (openBraceIndex == -2)
            {
                // found '}' instead of '{'
                success = false;
                break;
            }
            else if (openBraceIndex == -1)
            {
                // scanned the string and didn't find any remaining '{' or '}'
                break;
            }
#pragma warning restore S109 // Magic numbers should not be used

            int closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex + 1, endIndex);

            if (closeBraceIndex <= -1)
            {
                success = false;
                break;
            }

            // Format item syntax : { index[,alignment][ :formatString] }.
            var formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);
            var templateName = message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1).Trim();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                // braces with no named argument, such as "{}" and "{ }"
                success = false;
                break;
            }

            templates.Add(templateName);

            scanIndex = closeBraceIndex + 1;
        }

        return success;
    }

    /// <summary>
    /// Allows replacing individual template arguments with different strings.
    /// </summary>
    internal static string? MapTemplates(string? message, Func<string, string> mapTemplate)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var sb = new StringBuilder();

        var scanIndex = 0;
        var endIndex = message!.Length;
        while (scanIndex < endIndex)
        {
            var openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);

#pragma warning disable S109 // Magic numbers should not be used
            if (openBraceIndex == -2)
            {
                // found '}' instead of '{'
                break;
            }
            else if (openBraceIndex == -1)
            {
                // scanned the string and didn't find any remaining '{' or '}'
                break;
            }
#pragma warning restore S109 // Magic numbers should not be used

            var closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex + 1, endIndex);

            if (closeBraceIndex <= -1)
            {
                break;
            }

            // Format item syntax : { index[,alignment][ :formatString] }.
            var formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);

            var templateName = message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1).Trim();
            var mapped = mapTemplate(templateName);

            _ = sb.Append(message, scanIndex, openBraceIndex - scanIndex + 1);
            _ = sb.Append(mapped);
            _ = sb.Append(message, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

            scanIndex = closeBraceIndex + 1;
        }

        _ = sb.Append(message, scanIndex, message.Length - scanIndex);
        return sb.ToString();
    }

    internal static int FindIndexOfAny(string message, char[] chars, int startIndex, int endIndex)
    {
        var findIndex = message.IndexOfAny(chars, startIndex, endIndex - startIndex);
        return findIndex == -1 ? endIndex : findIndex;
    }

    /// <summary>
    /// Searches for the next brace index in the message.
    /// </summary>
    /// <remarks> The search skips any sequences of {{ or }}.</remarks>
    /// <example>{{prefix{{{Argument}}}suffix}}.</example>
    /// <returns>The zero-based index position of the first occurrence of the searched brace; -1 if the searched brace was not found; -2 if the wrong brace was found.</returns>
    private static int FindBraceIndex(string message, char searchedBrace, int startIndex, int endIndex)
    {
        Debug.Assert(searchedBrace is '{' or '}', "Searched brace must be { or }");

        int braceIndex = -1;
        int scanIndex = startIndex;

        while (scanIndex < endIndex)
        {
            char current = message[scanIndex];

            if (current is '{' or '}')
            {
                char currentBrace = current;

                int scanIndexBeforeSkip = scanIndex;
                while (current == currentBrace && ++scanIndex < endIndex)
                {
                    current = message[scanIndex];
                }

                int bracesCount = scanIndex - scanIndexBeforeSkip;
#pragma warning disable S109 // Magic numbers should not be used
                // if it is an even number of braces, just skip them, otherwise, we found an unescaped brace
                if (bracesCount % 2 != 0)
                {
                    if (currentBrace == searchedBrace)
                    {
                        if (currentBrace == '{')
                        {
                            // For '{' pick the last occurrence.
                            braceIndex = scanIndex - 1;
                        }
                        else
                        {
                            // For '}' pick the first occurrence.
                            braceIndex = scanIndexBeforeSkip;
                        }
                    }
                    else
                    {
                        // wrong brace found
                        braceIndex = -2;
                    }
#pragma warning restore S109 // Magic numbers should not be used

                    break;
                }
            }
            else
            {
                scanIndex++;
            }
        }

        return braceIndex;
    }
}

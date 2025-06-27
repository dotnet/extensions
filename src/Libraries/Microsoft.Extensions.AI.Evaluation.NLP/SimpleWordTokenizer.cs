// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// Tokenizes a string into segments using the common rules established by the NLTK word tokenizer.
/// </summary>
internal static class SimpleWordTokenizer
{
    /// <summary>
    /// Tokenizes the input text into individual words based on specific rules for text normalization and segmentation.
    /// </summary>
    /// <remarks>This method applies text normalization steps, such as removing skipped markers, handling line
    /// breaks, and replacing common HTML entities. It also ensures consistent tokenization by inserting spaces around
    /// punctuation, symbols, and certain character patterns. The tokenization rules are inspired by common BLEU algorithms,
    /// such as those used in NLTK, SacreBLEU, and MOSES.</remarks>
    /// <param name="text">The input text to be tokenized. Cannot be <see langword="null"/>.</param>
    /// <returns>An enumerable collection of strings, where each string represents a tokenized word. The collection will be empty
    /// if the input text contains no valid tokens.</returns>
    public static IEnumerable<string> WordTokenize(string text)
    {
        _ = Throw.IfNull(text, nameof(text));

        return WordTokenize(text.AsMemory());
    }

    /// <summary>
    /// Tokenizes the input text into individual words based on specific rules for text normalization and segmentation.
    /// </summary>
    /// <remarks>This method applies text normalization steps, such as removing skipped markers, handling line
    /// breaks, and replacing common HTML entities. It also ensures consistent tokenization by inserting spaces around
    /// punctuation, symbols, and certain character patterns. The tokenization rules are inspired by common BLEU algorithms,
    /// such as those used in NLTK, SacreBLEU, and MOSES.</remarks>
    /// <param name="text">The input text to be tokenized. Cannot be <see langword="null"/>.</param>
    /// <returns>An enumerable collection of strings, where each string represents a tokenized word. The collection will be empty
    /// if the input text contains no valid tokens.</returns>
    public static IEnumerable<string> WordTokenize(ReadOnlyMemory<char> text)
    {
        StringBuilder sb = new StringBuilder();

        while (true)
        {
            if (text.IsEmpty)
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    _ = sb.Clear();
                }

                yield break;
            }

            var span = text.Span;
            char nextChar = span[0];

            // Skip whitespace as separator
            if (char.IsWhiteSpace(nextChar))
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    _ = sb.Clear();
                }

                text = text.Slice(1);
                continue;
            }

            // Join hyphenated words
            if (span[0] == '-' &&
                span.Length > 1 &&
                span[1] == '\n')
            {
#pragma warning disable S109 // Magic numbers should not be used
                text = text.Slice(2);
#pragma warning restore S109 // Magic numbers should not be used
                continue;
            }

            // Translate HTML entities
            if (nextChar == '&')
            {
                if (span.StartsWith("&quot;".AsSpan()))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        _ = sb.Clear();
                    }

                    text = text.Slice("&quot;".Length);
                    yield return "\"";
                    continue;
                }
                else if (span.StartsWith("&amp;".AsSpan()))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        _ = sb.Clear();
                    }

                    text = text.Slice("&amp;".Length);
                    yield return "&";
                    continue;
                }
                else if (span.StartsWith("&lt;".AsSpan()))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        _ = sb.Clear();
                    }

                    text = text.Slice("&lt;".Length);
                    yield return "<";
                    continue;
                }
                else if (span.StartsWith("&gt;".AsSpan()))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        _ = sb.Clear();
                    }

                    text = text.Slice("&gt;".Length);
                    yield return ">";
                    continue;
                }
                else if (span.StartsWith("&apos;".AsSpan()))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        _ = sb.Clear();
                    }

                    text = text.Slice("&apos;".Length);
                    yield return "'";
                    continue;
                }
            }

            // Each symbol is a separate token
            if (char.IsSymbol(nextChar))
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    _ = sb.Clear();
                }

                yield return nextChar.ToString();
                text = text.Slice(1);
                continue;
            }

            // Return punctuation
            if (char.IsPunctuation(nextChar))
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    _ = sb.Clear();
                }

                yield return nextChar.ToString();
                text = text.Slice(1);
                continue;
            }

            // if we have a number, consume it along with any internal punctuation
            if (char.IsNumber(nextChar))
            {
                // in this case we are still building a token, then the number
                // should be added to the end of it, rather than as a separate number
                if (sb.Length > 0)
                {
                    _ = sb.Append(nextChar);
                    text = text.Slice(1);
                    continue;
                }

                while (!text.IsEmpty && (char.IsNumber(text.Span[0]) || char.IsPunctuation(text.Span[0])))
                {
                    _ = sb.Append(text.Span[0]);
                    text = text.Slice(1);
                }

                yield return sb.ToString();
                _ = sb.Clear();
                continue;
            }

            _ = sb.Append(char.ToUpperInvariant(nextChar));
            text = text.Slice(1);
        }

    }
}

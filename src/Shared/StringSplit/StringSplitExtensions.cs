// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET6_0

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Shared.StringSplit;

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static class StringSplitExtensions
{
    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">The string that delimits the substrings in this instance.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this ReadOnlySpan<char> input,
        string separator,
        Span<StringRange> result,
        out int numSegments,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
    {
        _ = Throw.IfNull(separator);
        CheckStringSplitOptions(options);

        numSegments = 0;

        int start = 0;
        while (true)
        {
            int index = -1;
            int separatorLen = 0;

            int found = input.Slice(start).IndexOf(separator.AsSpan(), comparison);
            if (found >= 0)
            {
                if (found < index || index < 0)
                {
                    separatorLen = separator.Length;
                    index = found;
                }
            }

            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                if (numSegments >= result.Length)
                {
                    return false;
                }

                result[numSegments++] = new StringRange(rangeStart, sp.Length);
            }

            if (index < 0)
            {
                return true;
            }

            start += index + separatorLen;
        }
    }

    private static void CheckStringSplitOptions(StringSplitOptions options)
    {
        const StringSplitOptions AllValidFlags = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        if ((options & ~AllValidFlags) != 0)
        {
            // at least one invalid flag was set
            Throw.ArgumentException(nameof(options), "Invalid split options specified");
        }
    }
}

#endif

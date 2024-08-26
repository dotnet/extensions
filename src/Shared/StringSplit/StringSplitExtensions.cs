// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER

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
    /// <param name="separator">A character that delimits the substrings in this instance.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this ReadOnlySpan<char> input,
        char separator,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        CheckStringSplitOptions(options);

        numSegments = 0;

        int start = 0;
        while (true)
        {
            int index = input.Slice(start).IndexOf(separator);
            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

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

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The characters that delimit the substrings in this instance. This is not treated as a string, this is used as an array of individual characters.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this ReadOnlySpan<char> input,
        ReadOnlySpan<char> separators,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        CheckStringSplitOptions(options);

        numSegments = 0;

        int start = 0;
        while (true)
        {
            int index = input.Slice(start).IndexOfAny(separators);
            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

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

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The strings that delimit the substrings in this instance.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this ReadOnlySpan<char> input,
        string[] separators,
        Span<StringRange> result,
        out int numSegments,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
    {
        _ = Throw.IfNull(separators);
        CheckStringSplitOptions(options);

        numSegments = 0;

        int start = 0;
        while (true)
        {
            int index = -1;
            int separatorLen = 0;
            foreach (var sep in separators)
            {
                int found = input.Slice(start).IndexOf(sep.AsSpan(), comparison);
                if (found >= 0)
                {
                    if (found < index || index < 0)
                    {
                        separatorLen = sep.Length;
                        index = found;
                    }
                }
            }

            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

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
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

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

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    /// <remarks>This uses whitespace as a separator of individual substrings.</remarks>
    public static bool TrySplit(
        this ReadOnlySpan<char> input,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        CheckStringSplitOptions(options);

        numSegments = 0;

        int start = 0;
        while (true)
        {
            int index = -1;
            for (int i = start; i < input.Length; i++)
            {
                if (char.IsWhiteSpace(input[i]))
                {
                    index = i - start;
                    break;
                }
            }

            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

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

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">A character that delimits the substrings in this instance.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this string input,
        char separator,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None)
        => TrySplit(input.AsSpan(), separator, result, out numSegments, options);

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The characters that delimit the substrings in this instance. This is not treated as a string, this is used as an array of individual characters.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this string input,
        ReadOnlySpan<char> separators,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None)
        => TrySplit(input.AsSpan(), separators, result, out numSegments, options);

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The strings that delimit the substrings in this instance.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    public static bool TrySplit(
        this string input,
        string[] separators,
        Span<StringRange> result,
        out int numSegments,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
        => TrySplit(input.AsSpan(), separators, result, out numSegments, comparison, options);

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
        this string input,
        string separator,
        Span<StringRange> result,
        out int numSegments,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
        => TrySplit(input.AsSpan(), separator, result, out numSegments, comparison, options);

    /// <summary>
    /// Splits a string into a number of string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="result">A span to receive the individual string segments.</param>
    /// <param name="numSegments">The number of string segments copied to the output.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns><see langword="true" /> if there was enough space in the output array; otherwise, <see langword="false" />.</returns>
    /// <remarks>This uses whitespace as a separator of individual substrings.</remarks>
    public static bool TrySplit(
        this string input,
        Span<StringRange> result,
        out int numSegments,
        StringSplitOptions options = StringSplitOptions.None) => TrySplit(input.AsSpan(), result, out numSegments, options);

    private static void CheckStringSplitOptions(StringSplitOptions options)
    {
#if NET5_0_OR_GREATER
        const StringSplitOptions AllValidFlags = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
#else
        const StringSplitOptions AllValidFlags = StringSplitOptions.RemoveEmptyEntries;
#endif

        if ((options & ~AllValidFlags) != 0)
        {
            // at least one invalid flag was set
            Throw.ArgumentException(nameof(options), "Invalid split options specified");
        }
    }

    /// <summary>
    /// The delegate that gets invoked when visiting the splits of a string.
    /// </summary>
    /// <typeparam name="T">Type of the context value given to the delegate.</typeparam>
    /// <param name="split">The span of characters that makes up the split.</param>
    /// <param name="segmentNum">The monotonically increasing split count.</param>
    /// <param name="context">The user-defined context object.</param>
    public delegate void SplitVisitor<T>(ReadOnlySpan<char> split, int segmentNum, T context);

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">A character that delimits the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this ReadOnlySpan<char> input,
        char separator,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        _ = Throw.IfNull(visitor);
        CheckStringSplitOptions(options);

        int numSegments = 0;
        int start = 0;
        while (true)
        {
            int index = input.Slice(start).IndexOf(separator);
            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                visitor(input.Slice(rangeStart, sp.Length), numSegments++, context);
            }

            if (index < 0)
            {
                return;
            }

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The characters that delimit the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this ReadOnlySpan<char> input,
        ReadOnlySpan<char> separators,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        _ = Throw.IfNull(visitor);
        CheckStringSplitOptions(options);

        int numSegments = 0;
        int start = 0;
        while (true)
        {
            int index = input.Slice(start).IndexOfAny(separators);
            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                visitor(input.Slice(rangeStart, sp.Length), numSegments++, context);
            }

            if (index < 0)
            {
                return;
            }

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The strings that delimit the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this ReadOnlySpan<char> input,
        string[] separators,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
    {
        _ = Throw.IfNull(separators);
        _ = Throw.IfNull(visitor);
        CheckStringSplitOptions(options);

        int numSegments = 0;
        int start = 0;
        while (true)
        {
            int index = -1;
            int separatorLen = 0;
            foreach (var sep in separators)
            {
                int found = input.Slice(start).IndexOf(sep.AsSpan(), comparison);
                if (found >= 0)
                {
                    if (found < index || index < 0)
                    {
                        separatorLen = sep.Length;
                        index = found;
                    }
                }
            }

            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                visitor(input.Slice(rangeStart, sp.Length), numSegments++, context);
            }

            if (index < 0)
            {
                return;
            }

            start += index + separatorLen;
        }
    }

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">The string that delimits the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void VisitSplits<TContext>(
        this ReadOnlySpan<char> input,
        string separator,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        _ = Throw.IfNull(separator);
        _ = Throw.IfNull(visitor);
        CheckStringSplitOptions(options);

        int numSegments = 0;
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
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                visitor(input.Slice(rangeStart, sp.Length), numSegments++, context);
            }

            if (index < 0)
            {
                return;
            }

            start += index + separatorLen;
        }
    }

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// This uses whitespace as a separator of individual substrings.
    ///
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this ReadOnlySpan<char> input,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
    {
        const int SeparatorLen = 1;

        _ = Throw.IfNull(visitor);
        CheckStringSplitOptions(options);

        int numSegments = 0;
        int start = 0;
        while (true)
        {
            int index = -1;
            for (int i = start; i < input.Length; i++)
            {
                if (char.IsWhiteSpace(input[i]))
                {
                    index = i - start;
                    break;
                }
            }

            var sp = index < 0 ? input.Slice(start) : input.Slice(start, index);

            var rangeStart = start;
#if NET5_0_OR_GREATER
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                var len = sp.Length;
                sp = sp.TrimStart();
                rangeStart = start + len - sp.Length;
                sp = sp.TrimEnd();
            }
#endif

            if (sp.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
            {
                visitor(input.Slice(rangeStart, sp.Length), numSegments++, context);
            }

            if (index < 0)
            {
                return;
            }

            start += index + SeparatorLen;
        }
    }

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">A character that delimits the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void VisitSplits<TContext>(
        this string input,
        char separator,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
        => VisitSplits(input.AsSpan(), separator, context, visitor, options);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The characters that delimit the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void VisitSplits<TContext>(
        this string input,
        ReadOnlySpan<char> separators,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
        => VisitSplits(input.AsSpan(), separators, context, visitor, options);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separators">The strings that delimit the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this string input,
        string[] separators,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
        => VisitSplits(input.AsSpan(), separators, context, visitor, comparison, options);

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="separator">The string that delimits the substrings in this instance.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="comparison">The kind of string comparison to apply to the separator strings.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this string input,
        string separator,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringComparison comparison = StringComparison.Ordinal,
        StringSplitOptions options = StringSplitOptions.None)
        => VisitSplits(input.AsSpan(), separator, context, visitor, comparison, options);

    /// <summary>
    /// Invokes a delegate for individual string segments.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="context">An object that can be used to pass state to the visitor.</param>
    /// <param name="visitor">A delegate that gets invoked for each individual segment.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <typeparam name="TContext">The type of the visitor's context.</typeparam>
    /// <remarks>
    /// This uses whitespace as a separator of individual substrings.
    ///
    /// The visitor delegate is invoked for each segment in the input. It is given as parameter the
    /// value of the <paramref name="context"/> argument, the segment index, and a range for the segment.
    /// </remarks>
    public static void VisitSplits<TContext>(
        this string input,
        TContext context,
        SplitVisitor<TContext> visitor,
        StringSplitOptions options = StringSplitOptions.None)
        => VisitSplits(input.AsSpan(), context, visitor, options);
}

#endif

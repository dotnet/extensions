// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Text;
#pragma warning restore CA1716

/// <summary>
/// Provides highly efficient string formatting functionality.
/// </summary>
/// <remarks>
/// This type lets you optimize string formatting operations common with the <see cref="string.Format(string,object?)" />
/// method. This is useful for any situation where you need to repeatedly format the same string with
/// different arguments.
///
/// This type works faster than <c>string.Format</c> because it parses the composite format string only once when
/// the instance is created, rather than doing it for every formatting operation.
///
/// You first create an instance of this type, passing the composite format string that you intend to use.
/// Once the instance is created, you call the <see cref="Format{T}(IFormatProvider?,T)"/> method with arguments to use in the
/// format operation.
///
/// You should only use this type if you need to repeatedly reuse the same composite format strings over time. If you only ever
/// use a composite format string once, you're better off using the original <c>string.Format</c> call.
/// </remarks>
#if !SHARED_PROJECT
[ExcludeFromCodeCoverage]
#endif

[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Acceptable use of magic numbers")]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
internal readonly struct CompositeFormat
{
    internal const int MaxStackAlloc = 128;  // = 256 bytes

    private readonly Segment[] _segments;     // info on the different chunks to process

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A classic .NET format string as used with <see cref="string.Format(string,object?)"  />.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <exception cref="ArgumentException">The format string is malformed.</exception>
    public static CompositeFormat Parse([StringSyntaxAttribute(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> format)
    {
        if (!TryParse(format, out var cf, out var error))
        {
            Throw.ArgumentException(nameof(format), error);
        }

        return cf;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A classic .NET format string as used with <see cref="string.Format(string,object?)"  />.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <exception cref="ArgumentException">The format string is malformed.</exception>
    public static CompositeFormat Parse([StringSyntaxAttribute(StringSyntaxAttribute.CompositeFormat)] string format)
        => Parse(format.AsSpan());

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A template-based .NET format string as used with <c>LogMethod.Define</c>.</param>
    /// <param name="templates">Holds the named templates discovered in the format string.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <exception cref="ArgumentException">The format string is malformed.</exception>
    public static CompositeFormat Parse([StringSyntaxAttribute(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> format, out IList<string> templates)
    {
        var l = new List<string>();

        if (!TryParse(format, l, out var cf, out var error))
        {
            Throw.ArgumentException(nameof(format), error);
        }

        templates = l;

        return cf;
    }

    private CompositeFormat(Segment[] segments, int numArgumentsNeeded, string literalString)
    {
        _segments = segments;
        NumArgumentsNeeded = numArgumentsNeeded;
        LiteralString = literalString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A classic .NET format string as used with <see cref="string.Format(string,object?)"  />.</param>
    /// <param name="result">Upon successful return, an initialized <see cref="CompositeFormat" /> instance.</param>
    /// <param name="error">Upon a failed return, a string providing details about the parsing error.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <returns><see langword="true"/> if the string parsed correctly, <see langword="false" /> otherwise.</returns>
    public static bool TryParse([StringSyntaxAttribute(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> format, out CompositeFormat result, [NotNullWhen(false)] out string? error)
    {
        return TryParse(format, null, out result, out error);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A classic .NET format string as used with <see cref="string.Format(string,object?)"  />.</param>
    /// <param name="result">Upon successful return, an initialized <see cref="CompositeFormat" /> instance.</param>
    /// <param name="error">Upon a failed return, a string providing details about the parsing error.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <returns><see langword="true"/> if the string parsed correctly, <see langword="false" /> otherwise.</returns>
    public static bool TryParse([StringSyntaxAttribute(StringSyntaxAttribute.CompositeFormat)] string format, out CompositeFormat result, [NotNullWhen(false)] out string? error)
    {
        return TryParse(format.AsSpan(), out result, out error);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFormat"/> struct.
    /// </summary>
    /// <param name="format">A template-based .NET format string as used with <c>LogMethod.Define</c>.</param>
    /// <param name="templates">Holds the named templates discovered in the format string.</param>
    /// <param name="result">Upon successful return, an initialized <see cref="CompositeFormat" /> instance.</param>
    /// <param name="error">Upon a failed return, a string providing details about the parsing error.</param>
    /// <remarks>
    /// Parses a composite format string into an efficient form for later use.
    /// </remarks>
    /// <returns><see langword="true"/> if the string parsed correctly, <see langword="false" /> otherwise.</returns>
    public static bool TryParse([StringSyntaxAttribute(
        StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> format,
        IList<string>? templates,
        out CompositeFormat result,
        [NotNullWhen(false)] out string? error)
    {
        var pos = 0;
        var len = format.Length;
        var ch = '\0';
        var segments = new List<Segment>();
        var numArgs = 0;
        using var literal = (format.Length >= MaxStackAlloc) ? new StringMaker(format.Length) : new StringMaker(stackalloc char[MaxStackAlloc]);

        result = default;
        error = null;

        while (true)
        {
            var segStart = literal.Length;
            while (pos < len)
            {
                ch = format[pos];

                pos++;
                if (ch == '}')
                {
                    if (pos < len && format[pos] == '}')
                    {
                        // double }, treat as escape sequence
                        pos++;
                    }
                    else
                    {
                        // dangling }, fail
                        error = $"Dangling }} in format string at position {pos}";
                        return false;
                    }
                }
                else if (ch == '{')
                {
                    if (pos < len && format[pos] == '{')
                    {
                        // double {, treat as escape sequence
                        pos++;
                    }
                    else
                    {
                        // start of a format specification
                        pos--;
                        break;
                    }
                }

                literal.Append(ch);
            }

            if (pos == len)
            {
                var totalLit = literal.Length - segStart;
                while (totalLit > 0)
                {
                    var num = Math.Min(totalLit, short.MaxValue);
                    segments.Add(new Segment((short)num, -1, 0, string.Empty));
                    totalLit -= num;
                }

                result = new CompositeFormat(segments.ToArray(), numArgs, literal.ExtractString());
                return true;
            }

            // extract the argument index
            var argIndex = 0;
            if (templates == null)
            {
                // classic composite format string

                pos++;
                if (pos == len || (ch = format[pos]) < '0' || ch > '9')
                {
                    // we need an argument index
                    error = $"Missing argument index in format string at position {pos}";
                    return false;
                }

                var start = pos;
                do
                {
                    argIndex = (argIndex * 10) + (ch - '0');

                    if (argIndex > short.MaxValue)
                    {
                        error = $"Argument index in format string at position {start} must be less than 32768";
                        return false;
                    }

                    pos++;

                    // make sure we get a suitable end to the argument index
                    if (pos == len)
                    {
                        error = $"Invalid character in format string argument index at position {pos}";
                        return false;
                    }

                    ch = format[pos];
                }
                while (ch >= '0' && ch <= '9');
            }
            else
            {
                // template-based format string

                pos++;
                if (pos == len)
                {
                    // we need a template name
                    error = $"Missing template name in format string at position {pos}";
                    return false;
                }

                ch = format[pos];
                if (!ValidTemplateNameChar(ch, true))
                {
                    // we need a template name
                    error = $"Missing template name in format string at position {pos}";
                    return false;
                }

                // extract the template name
                var start = pos;
                do
                {
                    pos++;

                    // make sure we get a suitable end
                    if (pos == len)
                    {
                        error = $"Invalid template name in format string at position {pos}";
                        return false;
                    }

                    ch = format[pos];
                }
                while (ValidTemplateNameChar(ch, false));

                // get an argument index for the given template
                var template = format.Slice(start, pos - start).ToString();
                argIndex = templates.IndexOf(template);
                if (argIndex < 0)
                {
                    templates.Add(template);
                    argIndex = numArgs;
                }
            }

            if (argIndex >= numArgs)
            {
                // new max arg count
                numArgs = argIndex + 1;
            }

            // skip whitespace
            while (pos < len && (ch = format[pos]) == ' ')
            {
                pos++;
            }

            // parse the optional field width
            var leftAligned = false;
            var argWidth = 0;
            if (ch == ',')
            {
                pos++;

                // skip whitespace
                while (pos < len && format[pos] == ' ')
                {
                    pos++;
                }

                // did we run out of steam
                if (pos == len)
                {
                    error = $"No field width found format string at position {pos}";
                    return false;
                }

                ch = format[pos];
                if (ch == '-')
                {
                    leftAligned = true;
                    pos++;

                    // did we run out of steam?
                    if (pos == len)
                    {
                        error = $"Invalid field width in format string at position {pos}";
                        return false;
                    }

                    ch = format[pos];
                }

                if (ch < '0' || ch > '9')
                {
                    error = $"Invalid character in field width in format string at position {pos}";
                    return false;
                }

                var val = 0;
                var start = pos;
                do
                {
                    val = (val * 10) + (ch - '0');
                    pos++;

                    // did we run out of steam?
                    if (pos == len)
                    {
                        error = $"Incomplete field width in format string at position {pos}";
                        return false;
                    }

                    // did we get a number that's too big?
                    if (val > short.MaxValue)
                    {
                        error = $"Field width in format string at position {start} must be less than 32768";
                        return false;
                    }

                    ch = format[pos];
                }
                while (ch >= '0' && ch <= '9');

                argWidth = val;
            }

            if (leftAligned)
            {
                argWidth = -argWidth;
            }

            // skip whitespace
            while (pos < len && (ch = format[pos]) == ' ')
            {
                pos++;
            }

            // parse the optional argument format string

            var argFormat = string.Empty;
            if (ch == ':')
            {
                pos++;
                var argFormatStart = pos;

                while (true)
                {
                    if (pos == len)
                    {
                        error = $"Unterminated format specification in format string at position {pos}";
                        return false;
                    }

                    ch = format[pos];
                    pos++;
                    if (ch == '{')
                    {
                        error = $"Nested {{ in format string at position {pos}";
                        return false;
                    }
                    else if (ch == '}')
                    {
                        // end of format specification
                        pos--;
                        break;
                    }
                }

                if (pos != argFormatStart)
                {
                    argFormat = format.Slice(argFormatStart, pos - argFormatStart).ToString();
                }
            }

            if (ch != '}')
            {
                error = "Unterminated format specification in format string at position {pos}";
                return false;
            }

            // skip over the closing brace
            pos++;

            var total = literal.Length - segStart;
            while (total > short.MaxValue)
            {
                segments.Add(new Segment(short.MaxValue, -1, 0, string.Empty));
                total -= short.MaxValue;
            }

            segments.Add(new Segment((short)total, (short)argIndex, (short)argWidth, argFormat));
        }
    }

    /// <summary>
    /// Formats a string with a single argument.
    /// </summary>
    /// <typeparam name="T">Type of the single argument.</typeparam>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg">An argument to use in the formatting operation.</param>
    /// <returns>The formatted string.</returns>
    public string Format<T>(IFormatProvider? provider, T arg)
    {
        CheckNumArgs(1, null);
        return Fmt<T, object?, object?>(provider, arg, null, null, null, EstimateArgSize(arg));
    }

    /// <summary>
    /// Formats a string with two arguments.
    /// </summary>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <returns>The formatted string.</returns>
    public string Format<T0, T1>(IFormatProvider? provider, T0 arg0, T1 arg1)
    {
        CheckNumArgs(2, null);
        return Fmt<T0, T1, object?>(provider, arg0, arg1, null, null, EstimateArgSize(arg0) + EstimateArgSize(arg1));
    }

    /// <summary>
    /// Formats a string with three arguments.
    /// </summary>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <typeparam name="T2">Type of the third argument.</typeparam>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <param name="arg2">Third argument to use in the formatting operation.</param>
    /// <returns>The formatted string.</returns>
    public string Format<T0, T1, T2>(IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2)
    {
        CheckNumArgs(3, null);
        return Fmt<T0, T1, T2>(provider, arg0, arg1, arg2, null, EstimateArgSize(arg0) + EstimateArgSize(arg1) + EstimateArgSize(arg2));
    }

    /// <summary>
    /// Formats a string with arguments.
    /// </summary>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <typeparam name="T2">Type of the third argument.</typeparam>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <param name="arg2">Third argument to use in the formatting operation.</param>
    /// <param name="args">Additional arguments to use in the formatting operation.</param>
    /// <returns>The formatted string.</returns>
    public string Format<T0, T1, T2>(IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        CheckNumArgs(3, args);
        return Fmt<T0, T1, T2>(provider, arg0, arg1, arg2, args, EstimateArgSize(arg0) + EstimateArgSize(arg1) + EstimateArgSize(arg2) + EstimateArgSize(args));
    }

    /// <summary>
    /// Formats a string with arguments.
    /// </summary>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="args">Arguments to use in the formatting operation.</param>
    /// <returns>The formatted string.</returns>
    public string Format(IFormatProvider? provider, params object?[]? args)
    {
        CheckNumArgs(0, args);

        if (NumArgumentsNeeded == 0)
        {
            return LiteralString;
        }

        var estimatedSize = EstimateArgSize(args);

#pragma warning disable CA1062 // Validate arguments of public methods - already handled by CheckNumArgs above
        return args!.Length switch
#pragma warning restore CA1062 // Validate arguments of public methods - already handled by CheckNumArgs above
        {
            1 => Fmt<object?, object?, object?>(provider, args[0], null, null, null, estimatedSize),
            2 => Fmt<object?, object?, object?>(provider, args[0], args[1], null, null, estimatedSize),
            3 => Fmt<object?, object?, object?>(provider, args[0], args[1], args[2], null, estimatedSize),
            _ => Fmt<object?, object?, object?>(provider, args[0], args[1], args[2], args.AsSpan(3), estimatedSize),
        };
    }

    /// <summary>
    /// Formats a string with one argument.
    /// </summary>
    /// <typeparam name="T">Type of the single argument.</typeparam>
    /// <param name="destination">Where to write the resulting string.</param>
    /// <param name="charsWritten">The number of characters actually written to the destination span.</param>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg">An argument to use in the formatting operation.</param>
    /// <returns>True if there was enough room in the destination span for the resulting string.</returns>
    public bool TryFormat<T>(Span<char> destination, out int charsWritten, IFormatProvider? provider, T arg)
    {
        CheckNumArgs(1, null);
        return TryFmt<T, object?, object?>(destination, out charsWritten, provider, arg, null, null, null);
    }

    /// <summary>
    /// Formats a string with two arguments.
    /// </summary>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <param name="destination">Where to write the resulting string.</param>
    /// <param name="charsWritten">The number of characters actually written to the destination span.</param>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <returns>True if there was enough room in the destination span for the resulting string.</returns>
    public bool TryFormat<T0, T1>(Span<char> destination, out int charsWritten, IFormatProvider? provider, T0 arg0, T1 arg1)
    {
        CheckNumArgs(2, null);
        return TryFmt<T0, T1, object?>(destination, out charsWritten, provider, arg0, arg1, null, null);
    }

    /// <summary>
    /// Formats a string with three arguments.
    /// </summary>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <typeparam name="T2">Type of the third argument.</typeparam>
    /// <param name="destination">Where to write the resulting string.</param>
    /// <param name="charsWritten">The number of characters actually written to the destination span.</param>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <param name="arg2">Third argument to use in the formatting operation.</param>
    /// <returns>True if there was enough room in the destination span for the resulting string.</returns>
    public bool TryFormat<T0, T1, T2>(Span<char> destination, out int charsWritten, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2)
    {
        CheckNumArgs(3, null);
        return TryFmt<T0, T1, T2>(destination, out charsWritten, provider, arg0, arg1, arg2, null);
    }

    /// <summary>
    /// Formats a string with arguments.
    /// </summary>
    /// <typeparam name="T0">Type of the first argument.</typeparam>
    /// <typeparam name="T1">Type of the second argument.</typeparam>
    /// <typeparam name="T2">Type of the third argument.</typeparam>
    /// <param name="destination">Where to write the resulting string.</param>
    /// <param name="charsWritten">The number of characters actually written to the destination span.</param>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="arg0">First argument to use in the formatting operation.</param>
    /// <param name="arg1">Second argument to use in the formatting operation.</param>
    /// <param name="arg2">Third argument to use in the formatting operation.</param>
    /// <param name="args">Additional arguments to use in the formatting operation.</param>
    /// <returns>True if there was enough room in the destination span for the resulting string.</returns>
    public bool TryFormat<T0, T1, T2>(Span<char> destination, out int charsWritten, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        CheckNumArgs(3, args);
        return TryFmt<T0, T1, T2>(destination, out charsWritten, provider, arg0, arg1, arg2, args);
    }

    /// <summary>
    /// Formats a string with arguments.
    /// </summary>
    /// <param name="destination">Where to write the resulting string.</param>
    /// <param name="charsWritten">The number of characters actually written to the destination span.</param>
    /// <param name="provider">An optional format provider that provides formatting functionality for individual arguments.</param>
    /// <param name="args">Arguments to use in the formatting operation.</param>
    /// <returns>True if there was enough room in the destination span for the resulting string.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, IFormatProvider? provider, params object?[]? args)
    {
        CheckNumArgs(0, args);

        if (NumArgumentsNeeded == 0)
        {
            if (destination.Length < LiteralString.Length)
            {
                charsWritten = 0;
                return false;
            }

            LiteralString.AsSpan().CopyTo(destination);
            charsWritten = LiteralString.Length;
            return true;
        }

#pragma warning disable CA1062 // Validate arguments of public methods - already handled by CheckNumArgs above
        return args!.Length switch
#pragma warning restore CA1062 // Validate arguments of public methods
        {
            1 => TryFmt<object?, object?, object?>(destination, out charsWritten, provider, args[0], null, null, null),
            2 => TryFmt<object?, object?, object?>(destination, out charsWritten, provider, args[0], args[1], null, null),
            3 => TryFmt<object?, object?, object?>(destination, out charsWritten, provider, args[0], args[1], args[2], null),
            _ => TryFmt(destination, out charsWritten, provider, args[0], args[1], args[2], args.AsSpan(3)),
        };
    }

    internal StringBuilder AppendFormat<T>(StringBuilder sb, IFormatProvider? provider, T arg)
    {
        CheckNumArgs(1, null);
        return AppendFmt<T, object?, object?>(sb, provider, arg, null, null, null, EstimateArgSize(arg));
    }

    internal StringBuilder AppendFormat<T0, T1>(StringBuilder sb, IFormatProvider? provider, T0 arg0, T1 arg1)
    {
        CheckNumArgs(2, null);
        return AppendFmt<T0, T1, object?>(sb, provider, arg0, arg1, null, null, EstimateArgSize(arg0) + EstimateArgSize(arg1));
    }

    internal StringBuilder AppendFormat<T0, T1, T2>(StringBuilder sb, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2)
    {
        CheckNumArgs(3, null);
        return AppendFmt<T0, T1, T2>(sb, provider, arg0, arg1, arg2, null, EstimateArgSize(arg0) + EstimateArgSize(arg1) + EstimateArgSize(arg2));
    }

    internal StringBuilder AppendFormat<T0, T1, T2>(StringBuilder sb, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        CheckNumArgs(3, args);
        return AppendFmt<T0, T1, T2>(sb, provider, arg0, arg1, arg2, args, EstimateArgSize(arg0) + EstimateArgSize(arg1) + EstimateArgSize(arg2) + EstimateArgSize(args));
    }

    internal StringBuilder AppendFormat(StringBuilder sb, IFormatProvider? provider, params object?[]? args)
    {
        CheckNumArgs(0, args);

        if (NumArgumentsNeeded == 0)
        {
            return sb.Append(LiteralString);
        }

        var estimatedSize = EstimateArgSize(args);

        return args!.Length switch
        {
            1 => AppendFmt<object?, object?, object?>(sb, provider, args[0], null, null, null, estimatedSize),
            2 => AppendFmt<object?, object?, object?>(sb, provider, args[0], args[1], null, null, estimatedSize),
            3 => AppendFmt<object?, object?, object?>(sb, provider, args[0], args[1], args[2], null, estimatedSize),
            _ => AppendFmt<object?, object?, object?>(sb, provider, args[0], args[1], args[2], args.AsSpan(3), estimatedSize),
        };
    }

    private static void AppendArg<T>(ref StringMaker sm, T arg, string argFormat, IFormatProvider? provider, int argWidth)
    {
        switch (arg)
        {
            case int a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case long a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case string a:
                sm.Append(a, argWidth);
                break;

            case double a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case float a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case uint a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case ulong a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case short a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case ushort a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case byte a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case sbyte a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case bool a:
                sm.Append(a, argWidth);
                break;

            case char a:
                sm.Append(a, argWidth);
                break;

            case decimal a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case DateTime a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case TimeSpan a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

#if NET6_0_OR_GREATER
            case System.ISpanFormattable a:
                sm.Append(a, argFormat, provider, argWidth);
                break;
#endif

            case IFormattable a:
                sm.Append(a, argFormat, provider, argWidth);
                break;

            case object a:
                sm.Append(a, argWidth);
                break;

            default:
                // when arg == null
                sm.Append(string.Empty, argWidth);
                break;
        }
    }

    private static bool ValidTemplateNameChar(char ch, bool first)
    {
        if (first)
        {
            return char.IsLetter(ch) || ch == '_';
        }

        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static int EstimateArgSize<T>(T arg)
    {
        var str = arg as string;
        if (str != null)
        {
            return str.Length;
        }

        return 8;
    }

    private static int EstimateArgSize(object?[]? args)
    {
        int total = 0;

        if (args != null)
        {
            foreach (var arg in args)
            {
                if (arg is string str)
                {
                    total += str.Length;
                }
            }
        }

        return total;
    }

    [SkipLocalsInit]
    private string Fmt<T0, T1, T2>(IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, ReadOnlySpan<object?> args, int estimatedSize)
    {
        estimatedSize += LiteralString.Length;
        var sm = (estimatedSize >= MaxStackAlloc) ? new StringMaker(estimatedSize) : new StringMaker(stackalloc char[MaxStackAlloc]);
        CoreFmt(ref sm, provider, arg0, arg1, arg2, args);
        return sm.ExtractString();
    }

    private bool TryFmt<T0, T1, T2>(Span<char> destination, out int charsWritten, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, ReadOnlySpan<object?> args)
    {
        var sm = new StringMaker(destination, true);
        CoreFmt(ref sm, provider, arg0, arg1, arg2, args);
        charsWritten = sm.Length;
        var overflowed = sm.Overflowed;
        return !overflowed;
    }

    [SkipLocalsInit]
    private StringBuilder AppendFmt<T0, T1, T2>(StringBuilder sb, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, ReadOnlySpan<object?> args, int estimatedSize)
    {
        estimatedSize += LiteralString.Length;
        var sm = (estimatedSize >= MaxStackAlloc) ? new StringMaker(estimatedSize) : new StringMaker(stackalloc char[MaxStackAlloc]);
        CoreFmt(ref sm, provider, arg0, arg1, arg2, args);
        sm.AppendTo(sb);
        sm.Dispose();
        return sb;
    }

    private void CheckNumArgs(int explicitCount, object?[]? args)
    {
        var total = explicitCount;
        if (args != null)
        {
            total += args.Length;
        }

        if (NumArgumentsNeeded > total)
        {
            Throw.ArgumentException(nameof(args), $"Expected {NumArgumentsNeeded} arguments, but got {total}");
        }
    }

    private void CoreFmt<T0, T1, T2>(ref StringMaker sm, IFormatProvider? provider, T0 arg0, T1 arg1, T2 arg2, ReadOnlySpan<object?> args)
    {
        var literalIndex = 0;
        foreach (var segment in _segments)
        {
            int literalCount = segment.LiteralCount;
            if (literalCount > 0)
            {
                // the segment has some literal text
                sm.Append(LiteralString.AsSpan(literalIndex, literalCount));
                literalIndex += literalCount;
            }

            var argIndex = segment.ArgIndex;
            if (argIndex >= 0)
            {
                // the segment has an arg to format
                switch (argIndex)
                {
                    case 0:
                        AppendArg(ref sm, arg0, segment.ArgFormat, provider, segment.ArgWidth);
                        break;

                    case 1:
                        AppendArg(ref sm, arg1, segment.ArgFormat, provider, segment.ArgWidth);
                        break;

                    case 2:
                        AppendArg(ref sm, arg2, segment.ArgFormat, provider, segment.ArgWidth);
                        break;

                    default:
                        AppendArg(ref sm, args[argIndex - 3], segment.ArgFormat, provider, segment.ArgWidth);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the number of arguments required in order to produce a string with this instance.
    /// </summary>
    public int NumArgumentsNeeded { get; }

    /// <summary>
    /// Gets all literal text to be inserted into the output.
    /// </summary>
    /// <remarks>
    /// In the case where the format string doesn't contain any formatting
    /// sequence, this literal is the string to produce when formatting.
    /// </remarks>
    private readonly string LiteralString { get; }
}

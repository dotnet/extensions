// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// Extended version of
/// <see href="https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs">SimpleConsoleFormatter</see>.
/// </summary>
internal static class TextWriterExtensions
{
    private const string ResetForegroundColor = "\x1B[39m\x1B[22m";
    private const string ResetBackgroundColor = "\x1B[49m";

    private const char Space = ' ';

    private static readonly char[] _digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    public static void Colorize(this TextWriter writer, string template, in ColorSet color, params object?[] param)
    {
        var span = template.AsSpan();
        var templateLength = template.Length;

        int? colorizeBegin = null;
        int? colorizeEnd = null;

        static string ReplaceParameters(ReadOnlySpan<char> message, params object?[] parameters)
        {
            var replaced = new StringBuilder();
            var expanding = false;
            var index = -1;

            foreach (var character in message)
            {
                if (character == '{')
                {
                    expanding = true;
                    continue;
                }

                if (expanding)
                {
                    if (character == '}')
                    {
                        if (index >= 0 &&
                            parameters.Length > index)
                        {
                            _ = replaced.Append(parameters[index]);
                        }

                        index = -1;
                        expanding = false;
                        continue;
                    }

                    if (char.IsNumber(character))
                    {
                        index = character - '0';
                        continue;
                    }

                    expanding = false;
                }

                _ = replaced.Append(character);
            }

            return replaced.ToString();
        }

        for (var i = 0; i < templateLength; i++)
        {
            if (span[i] == '{')
            {
                var isLookAheadPossible = i + 1 < templateLength;
                if (!isLookAheadPossible)
                {
                    continue;
                }

                var isNextDigit = _digits.Contains(span[i + 1]);
                if (isNextDigit)
                {
                    continue;
                }

                colorizeBegin = i;
            }

            if (!colorizeBegin.HasValue)
            {
                continue;
            }

            if (span[i] == '}')
            {
                var isLookBackPossible = i - 1 > 0;
                if (!isLookBackPossible)
                {
                    continue;
                }

                var previousIsDigit = _digits.Contains(span[i - 1]);
                if (previousIsDigit)
                {
                    continue;
                }

                colorizeEnd = i;
            }
        }

        if (colorizeBegin.HasValue && colorizeEnd.HasValue)
        {
            if (colorizeBegin.Value > 0)
            {
                var slice1 = span.Slice(0, colorizeBegin.Value);
                writer.Write(ReplaceParameters(slice1, param));
            }

            var slice2 = span.Slice(colorizeBegin.Value + 1, colorizeEnd.Value - colorizeBegin.Value - 1);
            writer.WriteColorImpl(ReplaceParameters(slice2, param), color);

            if (colorizeEnd < (templateLength - 1))
            {
                var slice3 = span.Slice(colorizeEnd.Value + 1, templateLength - colorizeEnd.Value - 1);
                writer.Write(ReplaceParameters(slice3, param));
            }
        }
        else
        {
            writer.Write(ReplaceParameters(span, param));
        }
    }

    public static void WriteCoordinate<T>(this TextWriter writer, IEnumerable<T> enumerable, in ColorSet color)
    {
        writer.WriteLine();
        writer.Colorize("{{0}:}", color, string.Join(":", enumerable));
        writer.WriteSpace();
    }

    public static void WriteSpace(this TextWriter writer) => writer.Write(Space);

    private static void WriteColorImpl<T>(this TextWriter writer, T message, in ColorSet colors)
        where T : notnull
    {
        if (colors.Foreground.HasValue)
        {
            writer.Write(GetForegroundColorEscapeCode(colors.Foreground.Value));
        }

        if (colors.Background.HasValue)
        {
            writer.Write(GetBackgroundColorEscapeCode(colors.Background.Value));
        }

        writer.Write(message.ToString());

        if (colors.Background.HasValue)
        {
            writer.Write(ResetBackgroundColor);
        }

        if (colors.Foreground.HasValue)
        {
            writer.Write(ResetForegroundColor);
        }
    }

    [ExcludeFromCodeCoverage]
    private static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.DarkGray => "\x1B[1m\x1B[30m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",
            _ => ResetForegroundColor
        };
    }

    [ExcludeFromCodeCoverage]
    private static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[40m",
            ConsoleColor.DarkRed => "\x1B[41m",
            ConsoleColor.DarkGreen => "\x1B[42m",
            ConsoleColor.DarkYellow => "\x1B[43m",
            ConsoleColor.DarkBlue => "\x1B[44m",
            ConsoleColor.DarkMagenta => "\x1B[45m",
            ConsoleColor.DarkCyan => "\x1B[46m",
            ConsoleColor.Gray => "\x1B[47m",
            _ => ResetBackgroundColor
        };
    }
}
#endif

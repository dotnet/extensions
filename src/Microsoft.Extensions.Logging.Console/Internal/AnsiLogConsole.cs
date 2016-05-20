// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console.Internal
{
    /// <summary>
    /// For non-Windows platform consoles which understand the ANSI escape code sequences to represent color
    /// </summary>
    public class AnsiLogConsole : IConsole
    {
        private readonly StringBuilder _outputBuilder;
        private readonly IAnsiSystemConsole _systemConsole;

        public AnsiLogConsole(IAnsiSystemConsole systemConsole)
        {
            _outputBuilder = new StringBuilder();
            _systemConsole = systemConsole;
        }

        public void Write(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
            if (background.HasValue)
            {
                _outputBuilder.Append(GetBackgroundColorEscapeCode(background.Value));
            }

            if (foreground.HasValue)
            {
                _outputBuilder.Append(GetForegroundColorEscapeCode(foreground.Value));
            }

            _outputBuilder.Append(message);

            if (foreground.HasValue)
            {
                _outputBuilder.Append("\x1B[39m"); // reset to default foreground color
            }

            if (background.HasValue)
            {
                _outputBuilder.Append("\x1B[0m"); // reset to the background color
            }
        }

        public void WriteLine(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            Write(message, background, foreground);
            _outputBuilder.AppendLine();
        }

        public void Flush()
        {
            _systemConsole.Write(_outputBuilder.ToString());
            _outputBuilder.Clear();
        }

        private static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black:
                    return "\x1B[30m";
                case ConsoleColor.DarkRed:
                    return "\x1B[31m";
                case ConsoleColor.DarkGreen:
                    return "\x1B[32m";
                case ConsoleColor.DarkYellow:
                    return "\x1B[33m";
                case ConsoleColor.DarkBlue:
                    return "\x1B[34m";
                case ConsoleColor.DarkMagenta:
                    return "\x1B[35m";
                case ConsoleColor.DarkCyan:
                    return "\x1B[36m";
                case ConsoleColor.Gray:
                    return "\x1B[37m";
                case ConsoleColor.Red:
                    return "\x1B[91m";
                case ConsoleColor.Green:
                    return "\x1B[92m";
                case ConsoleColor.Yellow:
                    return "\x1B[93m";
                case ConsoleColor.Blue:
                    return "\x1B[94m";
                case ConsoleColor.Magenta:
                    return "\x1B[95m";
                case ConsoleColor.Cyan:
                    return "\x1B[96m";
                case ConsoleColor.White:
                    return "\x1B[97m";
                default:
                    return "\x1B[39m"; // default foreground color
            }
        }

        private static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black:
                    return "\x1B[100m";
                case ConsoleColor.Red:
                    return "\x1B[101m";
                case ConsoleColor.Green:
                    return "\x1B[102m";
                case ConsoleColor.Yellow:
                    return "\x1B[103m";
                case ConsoleColor.Blue:
                    return "\x1B[104m";
                case ConsoleColor.Magenta:
                    return "\x1B[105m";
                case ConsoleColor.Cyan:
                    return "\x1B[106m";
                case ConsoleColor.White:
                    return "\x1B[107m";
                default:
                    return "\x1B[0m"; // Use default background color
            }
        }
    }
}

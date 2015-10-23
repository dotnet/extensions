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
            using (new BackgroundColorScope(_outputBuilder, background))
            {
                using (new ForegroundColorScope(_outputBuilder, foreground))
                {
                    _outputBuilder.Append(message);
                }
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

        private struct ForegroundColorScope : IDisposable
        {
            private readonly ConsoleColor? _foreground;
            private readonly StringBuilder _outputBuilder;

            public ForegroundColorScope(StringBuilder outputBuilder, ConsoleColor? foreground)
            {
                _foreground = foreground;
                _outputBuilder = outputBuilder;

                if (foreground.HasValue)
                {
                    outputBuilder.Append(GetForegroundColorEscapeCode(foreground.Value));
                }
            }

            public void Dispose()
            {
                if (_foreground.HasValue)
                {
                    _outputBuilder.Append("\x1B[39m"); // reset to default foreground color
                }
            }

            private static string GetForegroundColorEscapeCode(ConsoleColor color)
            {
                switch (color)
                {
                    case ConsoleColor.Red:
                        return "\x1B[31m";
                    case ConsoleColor.DarkGreen:
                        return "\x1B[32m";
                    case ConsoleColor.DarkYellow:
                        return "\x1B[33m";
                    case ConsoleColor.Gray:
                        return "\x1B[37m";
                    case ConsoleColor.White:
                        return "\x1B[97m";
                    default:
                        return "\x1B[39m"; // default foreground color
                }
            }
        }

        private struct BackgroundColorScope : IDisposable
        {
            private readonly ConsoleColor? _background;
            private readonly StringBuilder _outputBuilder;

            public BackgroundColorScope(StringBuilder outputBuilder, ConsoleColor? background)
            {
                _background = background;
                _outputBuilder = outputBuilder;

                if (background.HasValue)
                {
                    outputBuilder.Append(GetBackgroundColorEscapeCode(background.Value));
                }
            }

            public void Dispose()
            {
                if (_background.HasValue)
                {
                    _outputBuilder.Append("\x1B[0m"); // reset to the background color
                }
            }

            private static string GetBackgroundColorEscapeCode(ConsoleColor color)
            {
                switch (color)
                {
                    case ConsoleColor.Red:
                        return "\x1B[101m";
                    default:
                        return "\x1B[0m"; // Use default background color
                }
            }
        }
    }
}

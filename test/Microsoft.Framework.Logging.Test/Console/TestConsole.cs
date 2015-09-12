// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging.Console.Internal;

namespace Microsoft.Framework.Logging.Test.Console
{
    public class TestConsole : IConsole
    {
        public const ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;
        public const ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;

        private ConsoleSink _sink;

        public TestConsole(ConsoleSink sink)
        {
            _sink = sink;
        }

        public ConsoleColor BackgroundColor { get; set; } = DefaultBackgroundColor;

        public ConsoleColor ForegroundColor { get; set; } = DefaultForegroundColor;

        public void ResetColor()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }

        public void WriteLine(string message)
        {
            _sink.Write(new ConsoleContext()
            {
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor,
                Message = message
            });
        }
    }
}
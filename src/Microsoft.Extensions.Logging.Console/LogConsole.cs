// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Logging.Console.Internal;

namespace Microsoft.Extensions.Logging.Console
{
    public class LogConsole : IConsole
    {
        public ConsoleColor BackgroundColor
        {
            get
            {
                return System.Console.BackgroundColor;
            }

            set
            {
                System.Console.BackgroundColor = value;
            }
        }

        public ConsoleColor ForegroundColor
        {
            get
            {
                return System.Console.ForegroundColor;
            }

            set
            {
                System.Console.ForegroundColor = value;
            }
        }

        public void ResetColor()
        {
            System.Console.ResetColor();
        }

        public void WriteLine(string message)
        {
            System.Console.Error.WriteLine(message);
        }
    }
}
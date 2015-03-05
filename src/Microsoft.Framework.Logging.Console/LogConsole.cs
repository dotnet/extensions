using System;
using System.IO;
using Microsoft.Framework.Logging.Console.Internal;

namespace Microsoft.Framework.Logging.Console
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

        public void WriteLine(string message)
        {
            System.Console.Error.WriteLine(message);
        }
    }
}
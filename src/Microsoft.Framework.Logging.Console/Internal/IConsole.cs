using System;

namespace Microsoft.Framework.Logging.Console.Internal
{
    public interface IConsole
    {
        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }
        void WriteLine(string message);
    }
}
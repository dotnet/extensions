using System;
using Microsoft.Framework.Logging.Console;

namespace Microsoft.Framework.Logging.Test.Console
{
    public class TestConsole : IConsole
    {
        private ConsoleSink _sink;
        
        public TestConsole(ConsoleSink sink)
        {
            _sink = sink;
        }

        public ConsoleColor BackgroundColor { get; set; }

        public ConsoleColor ForegroundColor { get; set; }

        public void WriteLine(string format, params object[] args)
        {
            var message = string.Format(format, args);
            _sink.Write(new ConsoleContext()
            {
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor,
                Message = message
            });
        }
    }
}
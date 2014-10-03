using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging.Test.Console
{
    public class ConsoleSink
    {
        public ConsoleSink()
        {
            Writes = new List<ConsoleContext>();
        }

        public List<ConsoleContext> Writes { get; set; }

        public void Write(ConsoleContext context)
        {
            Writes.Add(context);
        }
    }
}
using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging.Test.Console
{
    public class ConsoleSink
    {
        public List<ConsoleContext> Writes { get; set; } = new List<ConsoleContext>();

        public void Write(ConsoleContext context)
        {
            Writes.Add(context);
        }
    }
}
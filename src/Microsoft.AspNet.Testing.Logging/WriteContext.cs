using System;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Testing.Logging
{
    public class WriteContext
    {
        public LogLevel LogLevel { get; set; }

        public int EventId { get; set; }

        public object State { get; set; }

        public Exception Exception { get; set; }

        public Func<object, Exception, string> Formatter { get; set; }

        public object Scope { get; set; }

        public string LoggerName { get; set; }
    }
}
using System;
using System.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Logging
{
    public static class TraceSourceFactoryExtensions
    {
        public static ILoggerFactory AddTraceSource(
            [NotNull] this ILoggerFactory factory,
            [NotNull] string switchName,
            [NotNull] TraceListener listener)
        {
            return factory.AddTraceSource(new SourceSwitch(switchName), listener);
        }

        public static ILoggerFactory AddTraceSource(
            [NotNull] this ILoggerFactory factory, 
            [NotNull] SourceSwitch sourceSwitch,
            [NotNull] TraceListener listener)
        {
            factory.AddProvider(new TraceSourceLoggerProvider(sourceSwitch, listener));

            return factory;
        }
    }
}
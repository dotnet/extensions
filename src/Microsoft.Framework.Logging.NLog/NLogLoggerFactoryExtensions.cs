using System;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Summary description for NLogLoggerFactoryExtensions
    /// </summary>
    public static class NLogLoggerFactoryExtensions
    {
        public static ILoggerFactory AddNLog(
            this ILoggerFactory factory,
            global::NLog.LogFactory logFactory)
        {
            factory.AddProvider(new NLog.NLogLoggerProvider(logFactory));
            return factory;
        }
    }
}
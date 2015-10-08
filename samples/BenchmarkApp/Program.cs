using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Internal;

namespace BenchmarkApp
{
    public class Program
    {
        const int ITERS = 10000;

        public void Main(string[] args)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            CurrentLoggingEnabled();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            WithLouisChangeLoggingEnabled();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            CurrentLoggingDisabled();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            WithLouisChangeLoggingDisabled();
        }

        public static void CurrentLoggingEnabled()
        {
            CurrentLogging(loggerEnabled: true);
        }

        public static void CurrentLoggingDisabled()
        {
            CurrentLogging(loggerEnabled: false);
        }

        public static void WithLouisChangeLoggingEnabled()
        {
            WithLouisChange(loggerEnabled: true);
        }

        public static void WithLouisChangeLoggingDisabled()
        {
            WithLouisChange(loggerEnabled: false);
        }

        public static void CurrentLogging(bool loggerEnabled = false)
        {
            var logger = new CustomLogger() { Enable = loggerEnabled };

            var requestId = Guid.NewGuid();
            var requestUrl = "http://test.com/api/values?p=10";
            var controller = "home";
            var action = "index";

            for (int i = 0; i < ITERS; i++)
            {
                // Operation A
                //_logger.LogVerbose("Request Id: {RequestId}", requestId);
                //_logger.LogVerbose("Request Id: {RequestId} with Url {Url}", requestId, requestUrl);
                logger.LogVerbose("Request matched controller '{controller}' and action '{action}'.", controller, action);
            }
        }

        public static void WithLouisChange(bool loggerEnabled = false)
        {
            var logger = new CustomLogger() { Enable = loggerEnabled };

            var requestId = Guid.NewGuid();
            var requestUrl = "http://test.com/api/values?p=10";
            var controller = "home";
            var action = "index";

            for (int i = 0; i < ITERS; i++)
            {
                // Operation A
                logger.RequestId(requestId);
                logger.RequestIdAndUrl(requestId, requestUrl);
                logger.ActionMatched(controller, action);
            }
        }
    }

    public class CustomLogger : ILogger
    {
        public bool Enable { get; set; } = true;

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Enable;
        }

        public void Log(
            LogLevel logLevel, int eventId, object state,
            Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            //do nothing
        }
    }
}
using System;
using Microsoft.Framework.Logging;

namespace BenchmarkApp
{
    public static class NewLoggerExtensions
    {
        private static Action<ILogger, string, string, Exception> _actionMatched;
        private static Action<ILogger, Guid, Exception> _requestId;
        private static Action<ILogger, Guid, string, Exception> _requestIdAndUrl;

        static NewLoggerExtensions()
        {
            LoggerMessage.Define(
                out _requestId,
                LogLevel.Information,
                eventId: 1,
                eventName: "RequestId",
                formatString: "Request Id: {RequestId}");

            LoggerMessage.Define(
                out _requestIdAndUrl,
                LogLevel.Information,
                eventId: 1,
                eventName: "RequestIdAndUrl",
                formatString: "Request Id: {RequestId} with Url {Url}");

            LoggerMessage.Define(
                out _actionMatched,
                LogLevel.Information,
                eventId: 1,
                eventName: "ActionMatch",
                formatString: "Request matched controller '{controller}' and action '{action}'.");
        }

        public static void RequestId(
            this ILogger logger, Guid requestId, Exception exception = null)
        {
            _requestId(logger, requestId, exception);
        }

        public static void RequestIdAndUrl(
            this ILogger logger, Guid requestId, string url, Exception exception = null)
        {
            _requestIdAndUrl(logger, requestId, url, exception);
        }

        public static void ActionMatched(
            this ILogger logger, string controller, string action, Exception exception = null)
        {
            _actionMatched(logger, controller, action, exception);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private ILogger _logger;

        public LoggingHttpMessageHandler(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var stopwatch = ValueStopwatch.StartNew();

            // Not using a scope here because we always expect this to be at the end of the pipeline, thus there's
            // not really anything to surround.
            Log.RequestStart(_logger, request);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Log.RequestEnd(_logger, response, stopwatch.GetElapsedTime());

            return response;
        }

        private static class Log
        {
            public static class EventIds
            {
                public static readonly EventId RequestStart = new EventId(100, "RequestStart");
                public static readonly EventId RequestEnd = new EventId(101, "RequestEnd");

                public static readonly EventId RequestHeader = new EventId(102, "RequestHeader");
                public static readonly EventId ResponseHeader = new EventId(103, "ResponseHeader");
            }

            private static readonly LogMessage<HttpMethod, Uri> _requestStart = (
                LogLevel.Information, 
                EventIds.RequestStart,
                "Sending HTTP request {HttpMethod} {Uri}");

            private static readonly LogMessage<double, HttpStatusCode> _requestEnd = (
                LogLevel.Information,
                EventIds.RequestEnd,
                "Received HTTP response after {ElapsedMilliseconds}ms - {StatusCode}");

            public static void RequestStart(ILogger logger, HttpRequestMessage request)
            {
                _requestStart.Log(logger, request.Method, request.RequestUri);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.Log(
                        LogLevel.Trace, 
                        EventIds.RequestHeader, 
                        new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, request.Headers, request.Content?.Headers),
                        null, 
                        (state, ex) => state.ToString());
                }
            }

            public static void RequestEnd(ILogger logger, HttpResponseMessage response, TimeSpan duration)
            {
                _requestEnd.Log(logger, duration.TotalMilliseconds, response.StatusCode);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.Log(
                        LogLevel.Trace, 
                        EventIds.ResponseHeader, 
                        new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Response, response.Headers, response.Content?.Headers), 
                        null, 
                        (state, ex) => state.ToString());
                }
            }
        }
    }
}
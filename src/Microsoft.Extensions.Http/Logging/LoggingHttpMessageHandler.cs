// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
            var response = await base.SendAsync(request, cancellationToken);
            Log.RequestEnd(_logger, response, stopwatch.GetElapsedTime());

            return response;
        }

        private static class Log
        {
            private static readonly Action<ILogger, HttpMethod, Uri, Exception> _requestStart = LoggerMessage.Define<HttpMethod, Uri>(
                LogLevel.Information, 
                EventIds.ClientHandlerRequestStart,
                "Sending HTTP request {HttpMethod} {Uri}");

            private static readonly Action<ILogger, double, HttpStatusCode, Exception> _requestEnd = LoggerMessage.Define<double, HttpStatusCode>(
                LogLevel.Information,
                EventIds.ClientHandlerRequestEnd,
                "Recieved HTTP response after {ElapsedMilliseconds}ms - {StatusCode}");

            private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _requestHeader = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Debug,
                EventIds.ClientHandlerRequestHeader,
                "Request header: '{HeaderName}' - '{HeaderNames}'");

            private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _responseHeader = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Debug,
                EventIds.ClientHandlerResponseHeader,
                "Response header: '{HeaderName}' - '{HeaderValues}'");

            public static void RequestStart(ILogger logger, HttpRequestMessage request)
            {
                _requestStart(logger, request.Method, request.RequestUri, null);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var header in request.Headers)
                    {
                        _requestHeader(logger, header.Key, header.Value, null);
                    }

                    if (request.Content != null)
                    {
                        foreach (var header in request.Content.Headers)
                        {
                            _requestHeader(logger, header.Key, header.Value, null);
                        }
                    }
                }
            }

            public static void RequestEnd(ILogger logger, HttpResponseMessage response, TimeSpan duration)
            {
                _requestEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var header in response.Headers)
                    {
                        _responseHeader(logger, header.Key, header.Value, null);
                    }

                    if (response.Content != null)
                    {
                        foreach (var header in response.Content.Headers)
                        {
                            _responseHeader(logger, header.Key, header.Value, null);
                        }
                    }
                }
            }
        }
    }
}
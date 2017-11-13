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
    public class LoggingScopeHttpMessageHandler : DelegatingHandler
    {
        private ILogger _logger;

        public LoggingScopeHttpMessageHandler(ILogger logger)
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

            using (Log.BeginRequestPipelineScope(_logger, request))
            {
                Log.RequestPipelineStart(_logger, request);
                var response = await base.SendAsync(request, cancellationToken);
                Log.RequestPipelineEnd(_logger, response, stopwatch.GetElapsedTime());

                return response;
            }
        }
        
        private static class Log
        {
            private static readonly Func<ILogger, HttpMethod, Uri, IDisposable> _beginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, Uri>("HTTP {HttpMethod} {Uri}");

            private static readonly Action<ILogger, HttpMethod, Uri, Exception> _requestPipelineStart = LoggerMessage.Define<HttpMethod, Uri>(
                LogLevel.Information, 
                EventIds.RequestPipelineStart, 
                "Start processing HTTP request {HttpMethod} {Uri}");

            private static readonly Action<ILogger, double, HttpStatusCode, Exception> _requestPipelineEnd = LoggerMessage.Define<double, HttpStatusCode>(
                LogLevel.Information, 
                EventIds.RequestPipelineEnd,
                "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");

            private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _requestHeader = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Debug,
                EventIds.RequestPipelineRequestHeader,
                "Request header: '{HeaderName}' - '{HeaderValues}'");

            private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _responseHeader = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Debug,
                EventIds.RequestPipelineResponseHeader,
                "Response header: '{HeaderName}' - '{HeaderValues}'");

            public static IDisposable BeginRequestPipelineScope(ILogger logger, HttpRequestMessage request)
            {
                return _beginRequestPipelineScope(logger, request.Method, request.RequestUri);
            }

            public static void RequestPipelineStart(ILogger logger, HttpRequestMessage request)
            {
                _requestPipelineStart(logger, request.Method, request.RequestUri, null);

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

            public static void RequestPipelineEnd(ILogger logger, HttpResponseMessage response, TimeSpan duration)
            {
                _requestPipelineEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);

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
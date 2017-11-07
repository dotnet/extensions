// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

            // Not using a scope here because we always expect this to be at the end of the pipeline, thus there's
            // not really anything to surround.
            Log.RequestStart(_logger, request);
            var response = await base.SendAsync(request, cancellationToken);
            Log.RequestEnd(_logger, response);

            return response;
        }

        private static class Log
        {
            private static readonly Action<ILogger, HttpMethod, Uri, Exception> _requestStart = LoggerMessage.Define<HttpMethod, Uri>(LogLevel.Information, EventIds.RequestStart, "Sending HTTP request {HttpMethod} {Uri}");
            private static readonly Action<ILogger, HttpMethod, Uri, HttpStatusCode, Exception> _requestEnd = LoggerMessage.Define<HttpMethod, Uri, HttpStatusCode>(LogLevel.Information, EventIds.RequestEnd, "Recieved HTTP response {HttpMethod} {Uri} - {StatusCode}");

            public static void RequestStart(ILogger logger, HttpRequestMessage request)
            {
                _requestStart(logger, request.Method, request.RequestUri, null);
            }

            public static void RequestEnd(ILogger logger, HttpResponseMessage response)
            {
                _requestEnd(logger, response.RequestMessage.Method, response.RequestMessage.RequestUri, response.StatusCode, null);
            }
        }
    }
}
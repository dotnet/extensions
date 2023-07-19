// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

internal sealed class HttpClientLogger : IHttpClientLogger
{
    private ILogger<HttpLoggingHandler> _logger;
    private IHttpRequestReader _httpRequestReader;
    private IEnumerable<IHttpClientLogEnricher> _enrichers;
    private IOptions<LoggingOptions> _loggingOptions;

    public HttpClientLogger(ILogger<HttpLoggingHandler> logger, IHttpRequestReader httpRequestReader, IEnumerable<IHttpClientLogEnricher> enrichers, IOptions<LoggingOptions> loggingOptions)
    {
        _logger = logger;
        _httpRequestReader = httpRequestReader;
        _enrichers = enrichers;
        _loggingOptions = loggingOptions;
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed) => throw new NotImplementedException();
    public object? LogRequestStart(HttpRequestMessage request) => throw new NotImplementedException();
    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed) => throw new NotImplementedException();
}

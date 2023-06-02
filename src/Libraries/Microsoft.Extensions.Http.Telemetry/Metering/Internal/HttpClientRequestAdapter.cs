// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK
using System;
#if !NET5_0_OR_GREATER
using System.Collections.Generic;
#endif
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Internal;

#pragma warning disable R9A013 // This class has virtual members and can't be sealed.
internal class HttpClientRequestAdapter
#pragma warning restore R9A013 // This class has virtual members and can't be sealed.
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private const string RequestStartTimeKey = "requestStartTimeTicks";

#if NET5_0_OR_GREATER
    private static readonly HttpRequestOptionsKey<DateTimeOffset> _requestStartTimeOptionsKey = new(RequestStartTimeKey);
#endif

    private readonly HttpMeteringHandler _httpMeteringHandler;

    public HttpClientRequestAdapter(HttpMeteringHandler httpMeteringHandler)
    {
        _httpMeteringHandler = httpMeteringHandler;
    }

    [DiagnosticName("System.Net.Http.HttpRequestOut")]
    public virtual void HttpClientListenerSubscribed(HttpRequestMessage request)
    {
        // This won't be invoked. This is needed just to add subscription for top level namespace,
        // because the http handler diagnostics listener check for this subscription to be present
        // before emitting request start or end events.
    }

    [DiagnosticName("System.Net.Http.HttpRequestOut.Start")]
    public virtual void OnRequestStart(HttpRequestMessage request)
    {
#if NET5_0_OR_GREATER
        request.Options.Set(_requestStartTimeOptionsKey, TimeProvider.GetUtcNow());
#else
        request.Properties[RequestStartTimeKey] = TimeProvider.GetUtcNow();
#endif
    }

    [DiagnosticName("System.Net.Http.HttpRequestOut.Stop")]
    public virtual void OnRequestStop(HttpResponseMessage? response, HttpRequestMessage request, TaskStatus requestTaskStatus)
    {
        if (requestTaskStatus == TaskStatus.Faulted)
        {
            // TaskStatus is faulted in case of any exceptions (except operation cancelled)
            // Metrics emission will be handled as part of the exception event in this case.
            return;
        }

        long durationInMs = GetRequestDuration(request);
        HttpStatusCode statusCode;
        if (response == null)
        {
            statusCode = requestTaskStatus == TaskStatus.Canceled ? HttpStatusCode.GatewayTimeout : HttpStatusCode.InternalServerError;
        }
        else
        {
            statusCode = response.StatusCode;
        }

        _httpMeteringHandler.OnRequestEnd(request, durationInMs, statusCode);
    }

    [DiagnosticName("System.Net.Http.Exception")]
    public virtual void OnRequestException(Exception exception, HttpRequestMessage request)
    {
        long durationInMs = GetRequestDuration(request);
        _httpMeteringHandler.OnRequestEnd(request, durationInMs, exception.GetStatusCode());
    }

    private long GetRequestDuration(HttpRequestMessage request)
    {
        long durationInMs = 0;

#if NET5_0_OR_GREATER
        if (request.Options.TryGetValue(_requestStartTimeOptionsKey, out var startTime))
#else
        if (request.Properties.TryGetValue(RequestStartTimeKey, out var startTimeObject) &&
            startTimeObject is DateTimeOffset startTime)
#endif
        {
            durationInMs = (long)(TimeProvider.GetUtcNow() - startTime).TotalMilliseconds;
        }

        return durationInMs;
    }
}
#endif

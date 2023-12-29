// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;

namespace Microsoft.Extensions.Http.Logging.Internal;

internal sealed class HttpHeadersReader : IHttpHeadersReader
{
    private readonly FrozenDictionary<string, DataClassification> _requestHeadersToLog;
    private readonly FrozenDictionary<string, DataClassification> _responseHeadersToLog;
    private readonly int _headersCountThreshold;
    private readonly IHttpHeadersRedactor _redactor;

    public HttpHeadersReader(IOptionsMonitor<LoggingOptions> optionsMonitor, IHttpHeadersRedactor redactor, [ServiceKey] string? serviceKey = null)
    {
        var options = optionsMonitor.GetKeyedOrCurrent(serviceKey);

        _redactor = redactor;

        _requestHeadersToLog = options.RequestHeadersDataClasses.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _responseHeadersToLog = options.ResponseHeadersDataClasses.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        _headersCountThreshold = _requestHeadersToLog.Count;
    }

    public void ReadRequestHeaders(HttpRequestMessage request, List<KeyValuePair<string, string>>? destination)
    {
        if (destination is null)
        {
            return;
        }

        ReadHeaders(request.Headers, _requestHeadersToLog, destination);
        if (request.Content is not null)
        {
            ReadHeaders(request.Content.Headers, _requestHeadersToLog, destination);
        }
    }

    public void ReadRequestHeadersNew(HttpRequestMessage request, List<KeyValuePair<string, string>>? destination)
    {
        if (destination is null)
        {
            return;
        }

        ReadHeadersNew(request.Headers, _requestHeadersToLog, destination);
        if (request.Content is not null)
        {
            ReadHeadersNew(request.Content.Headers, _requestHeadersToLog, destination);
        }
    }

    public void ReadResponseHeaders(HttpResponseMessage response, List<KeyValuePair<string, string>>? destination)
    {
        if (destination is null)
        {
            return;
        }

        ReadHeaders(response.Headers, _responseHeadersToLog, destination);
        if (response.Content is not null)
        {
            ReadHeaders(response.Content.Headers, _responseHeadersToLog, destination);
        }
    }

    private void ReadHeadersNew(HttpHeaders headers, FrozenDictionary<string, DataClassification> headersToLog, List<KeyValuePair<string, string>> destination)
    {
#if NET6_0_OR_GREATER
        var nonValidated = headers.NonValidated;
        if (nonValidated.Count == 0)
        {
            return;
        }

        if (nonValidated.Count < _headersCountThreshold)
        {
            // We have less headers than registered for logging, iterating over the smaller collection
            foreach (var header in headers)
            {
                if (headersToLog.TryGetValue(header.Key, out var classification))
                {
                    destination.Add(new(header.Key, _redactor.Redact(header.Value, classification)));
                }
            }

            return;
        }
#endif

        foreach (var kvp in headersToLog)
        {
            var classification = kvp.Value;
            var header = kvp.Key;

            if (headers.TryGetValues(header, out var values))
            {
                destination.Add(new(header, _redactor.Redact(values, classification)));
            }
        }
    }

    private void ReadHeaders(HttpHeaders headers, FrozenDictionary<string, DataClassification> headersToLog, List<KeyValuePair<string, string>> destination)
    {
        foreach (var kvp in headersToLog)
        {
            var classification = kvp.Value;
            var header = kvp.Key;

            if (headers.TryGetValues(header, out var values))
            {
                destination.Add(new(header, _redactor.Redact(values, classification)));
            }
        }
    }
}

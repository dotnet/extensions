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
    private readonly FrozenDictionary<string, DataClassification> _requestHeaders;
    private readonly FrozenDictionary<string, DataClassification> _responseHeaders;
    private readonly IHttpHeadersRedactor _redactor;

    public HttpHeadersReader(IOptionsMonitor<LoggingOptions> optionsMonitor, IHttpHeadersRedactor redactor, [ServiceKey] string? serviceKey = null)
    {
        var options = optionsMonitor.GetKeyedOrCurrent(serviceKey);

        _redactor = redactor;

        _requestHeaders = options.RequestHeadersDataClasses.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _responseHeaders = options.ResponseHeadersDataClasses.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public void ReadRequestHeaders(HttpRequestMessage request, List<KeyValuePair<string, string>>? destination)
    {
        if (destination is null)
        {
            return;
        }

        ReadHeaders(request.Headers, _requestHeaders, destination);
    }

    public void ReadResponseHeaders(HttpResponseMessage response, List<KeyValuePair<string, string>>? destination)
    {
        if (destination is null)
        {
            return;
        }

        ReadHeaders(response.Headers, _responseHeaders, destination);
    }

    private void ReadHeaders(HttpHeaders requestHeaders, FrozenDictionary<string, DataClassification> headersToLog, List<KeyValuePair<string, string>> destination)
    {
        foreach (var kvp in headersToLog)
        {
            var classification = kvp.Value;
            var header = kvp.Key;

            if (requestHeaders.TryGetValues(header, out var values))
            {
                destination.Add(new(header, _redactor.Redact(values, classification)));
            }
        }
    }
}

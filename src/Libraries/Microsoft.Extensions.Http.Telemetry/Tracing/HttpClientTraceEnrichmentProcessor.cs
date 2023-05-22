// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

internal sealed class HttpClientTraceEnrichmentProcessor
{
    private readonly IHttpClientTraceEnricher[] _traceEnrichers;

    public HttpClientTraceEnrichmentProcessor(IEnumerable<IHttpClientTraceEnricher> traceEnrichers)
    {
        _traceEnrichers = traceEnrichers.ToArray();
    }

    public void Enrich(Activity activity, HttpRequestMessage request, HttpResponseMessage? response)
    {
        foreach (var enricher in _traceEnrichers)
        {
            enricher.Enrich(activity, request, response);
        }
    }
}

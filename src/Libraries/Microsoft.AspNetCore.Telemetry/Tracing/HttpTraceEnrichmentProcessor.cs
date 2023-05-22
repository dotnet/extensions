// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Telemetry;

internal sealed class HttpTraceEnrichmentProcessor
{
    private readonly IHttpTraceEnricher[] _traceEnrichers;

    public HttpTraceEnrichmentProcessor(IEnumerable<IHttpTraceEnricher> traceEnrichers)
    {
        _traceEnrichers = traceEnrichers.ToArray();
    }

    public void Enrich(Activity activity, HttpRequest request)
    {
        foreach (var enricher in _traceEnrichers)
        {
            enricher.Enrich(activity, request);
        }
    }
}

#endif

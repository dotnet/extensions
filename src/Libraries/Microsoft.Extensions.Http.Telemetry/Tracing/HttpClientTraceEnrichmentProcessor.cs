// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using OpenTelemetry;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

internal sealed class HttpClientTraceEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly IHttpClientTraceEnricher[] _traceEnrichers;

    public HttpClientTraceEnrichmentProcessor(IEnumerable<IHttpClientTraceEnricher> traceEnrichers)
    {
        _traceEnrichers = traceEnrichers.ToArray();
    }

    public override void OnEnd(Activity activity)
    {
        var request = activity.GetRequest();
        var response = activity.GetResponse();

        if (request is null && response is null)
        {
            return;
        }

        activity.ClearResponse();

        foreach (var enricher in _traceEnrichers)
        {
            enricher.Enrich(activity, request, response);
        }
    }
}

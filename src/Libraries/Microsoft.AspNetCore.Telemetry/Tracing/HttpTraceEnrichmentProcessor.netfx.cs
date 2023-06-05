// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;

namespace Microsoft.AspNetCore.Telemetry;

internal sealed class HttpTraceEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly IHttpTraceEnricher[] _traceEnrichers;
    private readonly HttpUrlRedactionProcessor _redactionProcessor;

    public HttpTraceEnrichmentProcessor(HttpUrlRedactionProcessor redactionProcessor, IEnumerable<IHttpTraceEnricher> traceEnrichers)
    {
        _traceEnrichers = traceEnrichers.ToArray();
        _redactionProcessor = redactionProcessor;
    }

    public void EnrichAndRedact(Activity activity, HttpRequest request)
    {
        foreach (var enricher in _traceEnrichers)
        {
            enricher.Enrich(activity, request);
        }

        _redactionProcessor.ProcessRequest(activity, request);
        _redactionProcessor.ProcessResponse(activity, request);
    }

    public override void OnEnd(Activity activity)
    {
        HttpRequest? request = (HttpRequest?)activity.GetCustomProperty(Constants.CustomPropertyHttpRequest);

        if (request != null)
        {
            activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, null);

            EnrichAndRedact(activity, request);
        }
    }
}

#endif

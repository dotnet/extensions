// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Telemetry.Enrichment;
using OpenTelemetry;

namespace Microsoft.Extensions.Telemetry.Enrichment;

internal sealed class TraceEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly ITraceEnricher[] _traceEnrichers;

    public TraceEnrichmentProcessor(IEnumerable<ITraceEnricher> traceEnrichers)
    {
        _traceEnrichers = traceEnrichers.ToArray();
    }

#if NETCOREAPP3_1_OR_GREATER
    public override void OnStart(Activity activity)
    {
        foreach (var enricher in _traceEnrichers)
        {
            enricher.EnrichOnActivityStart(activity);
        }
    }
#endif
    public override void OnEnd(Activity activity)
    {
        foreach (var enricher in _traceEnrichers)
        {
            enricher.Enrich(activity);
        }
    }
}

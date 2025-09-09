// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// The enricher appends checkpoints for the outgoing http request.
/// It also logs the server name from the response header to correlate logs between client and server.
/// </summary>
internal sealed class HttpClientLatencyLogEnricher : IHttpClientLogEnricher
{
    private static readonly ObjectPool<StringBuilder> _builderPool = PoolFactory.SharedStringBuilderPool;
    private readonly HttpClientLatencyContext _latencyContext;
    private readonly HttpLatencyMediator _mediator;

    public HttpClientLatencyLogEnricher(HttpClientLatencyContext latencyContext, ILatencyContextTokenIssuer tokenIssuer,
        HttpLatencyMediator mediator)
    {
        _latencyContext = latencyContext;
        _mediator = mediator;
    }

    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        var lc = _latencyContext.Get();
        if (lc == null)
        {
            return;
        }
        
        // Use the mediator to record metrics
        _mediator.RecordRequest(lc, request);
        _mediator.RecordException(lc, exception);

        if (response != null)
        {
            // Record response metrics
            _mediator.RecordResponse(lc, response);
            
            StringBuilder stringBuilder = _builderPool.Get();

            // Add serverName to outgoing http logs
            AppendServerName(response.Headers, stringBuilder);
            stringBuilder.Append(',');
            
            // Use mediator to append checkpoint data
            _mediator.AppendCheckpoints(lc, stringBuilder);

            collector.Add("LatencyInfo", stringBuilder.ToString());

            _builderPool.Return(stringBuilder);
        }
    }

    private static void AppendServerName(HttpHeaders headers, StringBuilder stringBuilder)
    {
        if (headers.TryGetValues(TelemetryConstants.ServerApplicationNameHeader, out var values))
        {
            stringBuilder.Append(values!.First());
        }
    }
}

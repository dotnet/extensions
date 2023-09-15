// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Telemetry.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Internal;

/// <summary>
/// The enricher appends checkpoints for the outgoing http request.
/// It also logs the server name from the response header to correlate logs between client and server.
/// </summary>
internal sealed class HttpClientLatencyLogEnricher : IHttpClientLogEnricher
{
    private static readonly ObjectPool<StringBuilder> _builderPool = PoolFactory.SharedStringBuilderPool;
    private readonly HttpClientLatencyContext _latencyContext;

    private readonly CheckpointToken _enricherInvoked;

    public HttpClientLatencyLogEnricher(HttpClientLatencyContext latencyContext, ILatencyContextTokenIssuer tokenIssuer)
    {
        _latencyContext = latencyContext;
        _enricherInvoked = tokenIssuer.GetCheckpointToken(HttpCheckpoints.EnricherInvoked);
    }

    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        if (response != null)
        {
            var lc = _latencyContext.Get();
            lc?.AddCheckpoint(_enricherInvoked);

            StringBuilder stringBuilder = _builderPool.Get();

            // Add serverName, checkpoints to outgoing http logs.
            AppendServerName(response.Headers, stringBuilder);
            _ = stringBuilder.Append(',');

            if (lc != null)
            {
                AppendCheckpoints(lc, stringBuilder);
            }

            collector.Add("latencyInfo", stringBuilder.ToString());

            _builderPool.Return(stringBuilder);
        }
    }

    private static void AppendServerName(HttpHeaders headers, StringBuilder stringBuilder)
    {
        if (headers.TryGetValues(TelemetryConstants.ServerApplicationNameHeader, out var values))
        {
            _ = stringBuilder.Append(values!.First());
        }
    }

    private static void AppendCheckpoints(ILatencyContext latencyContext, StringBuilder stringBuilder)
    {
        var latencyData = latencyContext.LatencyData;
        for (int i = 0; i < latencyData.Checkpoints.Length; i++)
        {
            _ = stringBuilder.Append(latencyData.Checkpoints[i].Name);
            _ = stringBuilder.Append('/');
        }

        _ = stringBuilder.Append(',');
        for (int i = 0; i < latencyData.Checkpoints.Length; i++)
        {
            var ms = ((double)latencyData.Checkpoints[i].Elapsed / latencyData.Checkpoints[i].Frequency) * 1000;
            _ = stringBuilder.Append(ms);
            _ = stringBuilder.Append('/');
        }
    }
}

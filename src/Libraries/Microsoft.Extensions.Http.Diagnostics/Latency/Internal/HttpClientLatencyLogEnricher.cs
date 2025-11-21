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
    private readonly CheckpointToken _enricherInvoked;

    public HttpClientLatencyLogEnricher(
        HttpClientLatencyContext latencyContext,
        ILatencyContextTokenIssuer tokenIssuer,
        HttpLatencyMediator _ /* mediator no longer needed for record end here */)
    {
        _latencyContext = latencyContext;
        _enricherInvoked = tokenIssuer.GetCheckpointToken(HttpCheckpoints.EnricherInvoked);
    }

    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage? request, HttpResponseMessage? response, Exception? exception)
    {
        if (response == null)
        {
            return;
        }

        var live = _latencyContext.Get();
        LatencySnapshot? snapshot = null;

        if (live == null && request != null)
        {
            _ = HttpRequestLatencySnapshotStore.TryGet(request, out snapshot);
        }

        if (live != null)
        {
            // Final checkpoint for the logging enrichment only (no record-end here).
            live.AddCheckpoint(_enricherInvoked);
        }

        var sb = _builderPool.Get();
        try
        {
            _ = sb.Append("v1.0").Append(',');
            AppendServerName(response.Headers, sb);
            _ = sb.Append(',');

            if (live != null)
            {
                AppendTags(live.LatencyData, sb); _ = sb.Append(',');
                AppendCheckpoints(live.LatencyData, sb); _ = sb.Append(',');
                AppendMeasures(live.LatencyData, sb);
            }
            else if (snapshot != null)
            {
                AppendTags(snapshot, sb); _ = sb.Append(',');
                AppendCheckpoints(snapshot, sb); _ = sb.Append(',');
                AppendMeasures(snapshot, sb);
            }

            collector.Add("LatencyInfo", sb.ToString());
        }
        finally
        {
            _builderPool.Return(sb);
        }
    }

    private static void AppendServerName(HttpHeaders headers, StringBuilder sb)
    {
        if (headers.TryGetValues(TelemetryConstants.ServerApplicationNameHeader, out var values))
        {
            _ = sb.Append(values.First());
        }
    }

    private static void AppendTags(in LatencyData data, StringBuilder sb)
    {
        var span = data.Tags;
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(span[i].Name).Append('/');
        }

        _ = sb.Append(',');
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(span[i].Value).Append('/');
        }
    }

    private static void AppendCheckpoints(in LatencyData data, StringBuilder sb)
    {
        const int MsPerSec = 1000;
        var span = data.Checkpoints;
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(span[i].Name).Append('/');
        }

        _ = sb.Append(',');
        for (int i = 0; i < span.Length; i++)
        {
            var cp = span[i];
            var ms = (long)Math.Round(((double)cp.Elapsed / cp.Frequency) * MsPerSec);
            _ = sb.Append(ms).Append('/');
        }
    }

    private static void AppendMeasures(in LatencyData data, StringBuilder sb)
    {
        var span = data.Measures;
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(span[i].Name).Append('/');
        }

        _ = sb.Append(',');
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(span[i].Value).Append('/');
        }
    }

    // Snapshot overloads
    private static void AppendTags(LatencySnapshot snapshot, StringBuilder sb)
    {
        var arr = snapshot.Tags;
        for (int i = 0; i < arr.Length; i++)
        {
            _ = sb.Append(arr[i].Name).Append('/');
        }

        _ = sb.Append(',');
        for (int i = 0; i < arr.Length; i++)
        {
            _ = sb.Append(arr[i].Value).Append('/');
        }
    }

    private static void AppendCheckpoints(LatencySnapshot snapshot, StringBuilder sb)
    {
        const int MsPerSec = 1000;
        var arr = snapshot.Checkpoints;
        for (int i = 0; i < arr.Length; i++)
        {
            _ = sb.Append(arr[i].Name).Append('/');
        }

        _ = sb.Append(',');
        for (int i = 0; i < arr.Length; i++)
        {
            var cp = arr[i];
            var ms = (long)Math.Round(((double)cp.Elapsed / cp.Frequency) * MsPerSec);
            _ = sb.Append(ms).Append('/');
        }
    }

    private static void AppendMeasures(LatencySnapshot snapshot, StringBuilder sb)
    {
        var arr = snapshot.Measures;
        foreach (var t in arr)
        {
            _ = sb.Append(t.Name).Append('/');
        }

        _ = sb.Append(',');
        foreach (var t in arr)
        {
            _ = sb.Append(t.Value).Append('/');
        }
    }
}
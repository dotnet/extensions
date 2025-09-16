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
    private readonly HttpLatencyMediator _httpLatencyMediator;
    private readonly CheckpointToken _enricherInvoked;

    public HttpClientLatencyLogEnricher(
        HttpClientLatencyContext latencyContext,
        ILatencyContextTokenIssuer tokenIssuer,
        HttpLatencyMediator httpLatencyMediator)
    {
        _latencyContext = latencyContext;
        _httpLatencyMediator = httpLatencyMediator;
        _enricherInvoked = tokenIssuer.GetCheckpointToken(HttpCheckpoints.EnricherInvoked);
    }

    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage? request, HttpResponseMessage? response, Exception? exception)
    {
        if (response != null)
        {
            var lc = _latencyContext.Get();

            if (lc != null)
            {
                // Add the checkpoint
                lc.AddCheckpoint(_enricherInvoked);

                // Use the mediator to record all metrics
                _httpLatencyMediator.RecordEnd(lc, response);
            }

            StringBuilder stringBuilder = _builderPool.Get();

            try
            {
                /* Add version, serverName, checkpoints, and measures to outgoing http logs.
                 * Schemas: 1) ServerName,CheckpointName,CheckpointValue
                 *          2) v1.0,ServerName,TagName,TagValue,CheckpointName,CheckpointValue,MetricName,MetricValue
                 */

                // Add version
                _ = stringBuilder.Append("v1.0");
                _ = stringBuilder.Append(',');

                // Add server name
                AppendServerName(response.Headers, stringBuilder);
                _ = stringBuilder.Append(',');

                // Add tags, checkpoints, and measures
                if (lc != null)
                {
                    AppendTags(lc, stringBuilder);
                    _ = stringBuilder.Append(',');

                    AppendCheckpoints(lc, stringBuilder);
                    _ = stringBuilder.Append(',');

                    AppendMeasures(lc, stringBuilder);
                }

                collector.Add("LatencyInfo", stringBuilder.ToString());
            }
            finally
            {
                _builderPool.Return(stringBuilder);
            }
        }
    }

    private static void AppendServerName(HttpHeaders headers, StringBuilder stringBuilder)
    {
        if (headers.TryGetValues(TelemetryConstants.ServerApplicationNameHeader, out var values))
        {
            _ = stringBuilder.Append(values.First());
        }
    }

    private static void AppendCheckpoints(ILatencyContext latencyContext, StringBuilder stringBuilder)
    {
        const int MillisecondsPerSecond = 1000;

        var latencyData = latencyContext.LatencyData;
        var checkpointCount = latencyData.Checkpoints.Length;

        for (int i = 0; i < checkpointCount; i++)
        {
            _ = stringBuilder.Append(latencyData.Checkpoints[i].Name);
            _ = stringBuilder.Append('/');
        }

        _ = stringBuilder.Append(',');

        for (int i = 0; i < checkpointCount; i++)
        {
            var cp = latencyData.Checkpoints[i];
            _ = stringBuilder.Append((long)Math.Round(((double)cp.Elapsed / cp.Frequency) * MillisecondsPerSecond));
            _ = stringBuilder.Append('/');
        }
    }

    private static void AppendMeasures(ILatencyContext latencyContext, StringBuilder stringBuilder)
    {
        var latencyData = latencyContext.LatencyData;
        var measureCount = latencyData.Measures.Length;

        for (int i = 0; i < measureCount; i++)
        {
            _ = stringBuilder.Append(latencyData.Measures[i].Name);
            _ = stringBuilder.Append('/');
        }

        _ = stringBuilder.Append(',');

        for (int i = 0; i < measureCount; i++)
        {
            _ = stringBuilder.Append(latencyData.Measures[i].Value);
            _ = stringBuilder.Append('/');
        }
    }

    private static void AppendTags(ILatencyContext latencyContext, StringBuilder stringBuilder)
    {
        var latencyData = latencyContext.LatencyData;
        var tagCount = latencyData.Tags.Length;

        for (int i = 0; i < tagCount; i++)
        {
            _ = stringBuilder.Append(latencyData.Tags[i].Name);
            _ = stringBuilder.Append('/');
        }

        _ = stringBuilder.Append(',');

        for (int i = 0; i < tagCount; i++)
        {
            _ = stringBuilder.Append(latencyData.Tags[i].Value);
            _ = stringBuilder.Append('/');
        }
    }
}
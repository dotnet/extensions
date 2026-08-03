// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Enriches incoming HTTP request logs with latency information captured in the request's <see cref="ILatencyContext"/>.
/// </summary>
internal sealed class HttpLatencyLogEnricher : IHttpLogEnricher
{
    internal const string DataVersion = "v1.0";

    private static readonly ObjectPool<StringBuilder> _builderPool = PoolFactory.SharedStringBuilderPool;

    public void Enrich(IEnrichmentTagCollector collector, HttpContext httpContext)
    {
        var latencyContext = httpContext.RequestServices.GetService<ILatencyContext>();

        if (latencyContext != null)
        {
            StringBuilder stringBuilder = _builderPool.Get();
            try
            {
                _ = stringBuilder.Append(DataVersion);
                _ = stringBuilder.Append(',');
                AppendClientName(httpContext.Request, stringBuilder);
                _ = stringBuilder.Append(',');
                FormatLatencyData(stringBuilder, latencyContext.LatencyData);
                collector.Add("LatencyInfo", stringBuilder.ToString());
            }
            finally
            {
                _builderPool.Return(stringBuilder);
            }
        }
    }

    private static void AppendClientName(HttpRequest request, StringBuilder stringBuilder)
    {
        if (request.Headers.TryGetValue(TelemetryConstants.ClientApplicationNameHeader, out var values))
        {
            _ = stringBuilder.Append(values[0]);
        }
    }

    private static void FormatLatencyData(StringBuilder sb, LatencyData latencyData)
    {
        const int MillisecondsPerSecond = 1000;

        // Append tags
        AppendSpanEscapingSlash(sb, latencyData.Tags, a => a.Name);
        _ = sb.Append(',');
        AppendSpanEscapingSlash(sb, latencyData.Tags, a => a.Value);
        _ = sb.Append(',');

        // Append checkpoints
        AppendSpanEscapingSlash(sb, latencyData.Checkpoints, a => a.Name);
        _ = sb.Append(',');
        AppendSpan(sb, latencyData.Checkpoints, a => (long)Math.Round(((double)a.Elapsed / a.Frequency) * MillisecondsPerSecond));
        _ = sb.Append(',');

        // Append measures
        AppendSpanEscapingSlash(sb, latencyData.Measures, a => a.Name);
        _ = sb.Append(',');
        AppendSpan(sb, latencyData.Measures, a => a.Value);
        _ = sb.Append(',');

        // Append duration
        _ = sb.Append((long)Math.Round(((double)latencyData.DurationTimestamp / latencyData.DurationTimestampFrequency) * MillisecondsPerSecond));
    }

    private static void AppendSpanEscapingSlash<TX>(StringBuilder sb, ReadOnlySpan<TX> span, Func<TX, string> select)
    {
        for (int i = 0; i < span.Length; i++)
        {
            var selectedValue = select(span[i]).AsSpan();
            for (int s = 0; s < selectedValue.Length; s++)
            {
                _ = sb.Append(selectedValue[s] == '/' ? '_' : selectedValue[s]);
            }

            _ = sb.Append('/');
        }
    }

    private static void AppendSpan<TX, TY>(StringBuilder sb, ReadOnlySpan<TX> span, Func<TX, TY> apply)
    {
        for (int i = 0; i < span.Length; i++)
        {
            _ = sb.Append(apply(span[i]));
            _ = sb.Append('/');
        }
    }
}

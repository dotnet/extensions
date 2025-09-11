// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Mediator for HTTP latency operations that coordinates recording HTTP metrics in a latency context.
/// </summary>
internal sealed class HttpLatencyMediator
{
#if !NETFRAMEWORK
    private readonly MeasureToken _gcPauseTime;
#endif
    private readonly TagToken _httpVersionTag;

    public HttpLatencyMediator(ILatencyContextTokenIssuer tokenIssuer)
    {
#if !NETFRAMEWORK
        _gcPauseTime = tokenIssuer.GetMeasureToken(HttpMeasures.GCPauseTime);
#endif
        _httpVersionTag = tokenIssuer.GetTagToken(HttpTags.HttpVersion);
    }

#pragma warning disable CA1822
    public void RecordStart(ILatencyContext latencyContext, HttpRequestMessage? request = null, HttpResponseMessage? response = null)
    {
#pragma warning restore CA1822
#if NET
        latencyContext.RecordMeasure(_gcPauseTime, (long)GC.GetTotalPauseDuration().TotalMilliseconds * -1L);
#endif
    }

    public void RecordEnd(ILatencyContext latencyContext, HttpRequestMessage? request = null, HttpResponseMessage? response = null)
    {
#if NET
        latencyContext.AddMeasure(_gcPauseTime, (long)GC.GetTotalPauseDuration().TotalMilliseconds);
#endif
        if (response != null)
        {
            latencyContext.SetTag(_httpVersionTag, response.Version.ToString());
        }
    }
}


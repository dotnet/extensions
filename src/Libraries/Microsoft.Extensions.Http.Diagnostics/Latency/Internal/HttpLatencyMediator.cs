// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Mediator for HTTP latency operations that coordinates recording HTTP metrics in a latency context.
/// </summary>
internal sealed class HttpLatencyMediator
{
    private readonly MeasureToken _gcPauseTime;
    private readonly TagToken _httpVersionTag;

    public HttpLatencyMediator(ILatencyContextTokenIssuer tokenIssuer)
    {
        _gcPauseTime = tokenIssuer.GetMeasureToken(HttpMeasures.GCPauseTime);
        _httpVersionTag = tokenIssuer.GetTagToken(HttpTags.HttpVersion);
    }

    public void RecordStart(ILatencyContext latencyContext)
    {
        latencyContext.RecordMeasure(_gcPauseTime, (long)System.GC.GetTotalPauseDuration().TotalMilliseconds * -1L);
    }

    public void RecordEnd(ILatencyContext latencyContext, HttpResponseMessage? response = null)
    {
        latencyContext.AddMeasure(_gcPauseTime, (long)System.GC.GetTotalPauseDuration().TotalMilliseconds);

        if (response != null)
        {
            latencyContext.SetTag(_httpVersionTag, response.Version.ToString());
        }
    }
}
#endif

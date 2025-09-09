// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Mediator for HTTP latency operations that coordinates recording HTTP metrics in a latency context.
/// </summary>
internal class HttpLatencyMediator
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


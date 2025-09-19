// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if !NET

using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

internal sealed class HttpLatencyMediator
{
    private readonly TagToken _httpVersionTag;

    public HttpLatencyMediator(ILatencyContextTokenIssuer tokenIssuer)
    {
        _httpVersionTag = tokenIssuer.GetTagToken(HttpTags.HttpVersion);
    }

    public void RecordEnd(ILatencyContext latencyContext, HttpResponseMessage? response = null)
    {
        if (response != null)
        {
            latencyContext?.SetTag(_httpVersionTag, response.Version.ToString());
        }
    }
}
#endif
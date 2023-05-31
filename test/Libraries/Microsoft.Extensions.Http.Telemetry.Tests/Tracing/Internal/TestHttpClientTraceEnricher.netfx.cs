// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

internal sealed class TestHttpClientTraceEnricher : IHttpClientTraceEnricher
{
    public TestHttpClientTraceEnricher(IOptions<HttpClientTracingOptions> _)
    {
    }

    public void Enrich(Activity activity, HttpWebRequest? webRequest, HttpWebResponse? webResponse)
    {
        // no op.
    }
}

#endif

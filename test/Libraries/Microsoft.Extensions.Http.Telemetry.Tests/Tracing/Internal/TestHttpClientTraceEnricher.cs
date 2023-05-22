// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

#pragma warning disable SA1402 // File may only contain a single type

internal sealed class TestHttpClientTraceEnricher : IHttpClientTraceEnricher
{
    public TestHttpClientTraceEnricher(IOptions<HttpClientTracingOptions> _)
    {
    }

    public void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response) => Assert.NotNull(request);
}

internal sealed class TestHttpClientResponseTraceEnricher : IHttpClientTraceEnricher
{
    public TestHttpClientResponseTraceEnricher(IOptions<HttpClientTracingOptions> _)
    {
        // nop
    }

    public void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        // nop
    }
}

internal sealed class TestHttpClientResponseTraceEnricher2 : IHttpClientTraceEnricher
{
    public TestHttpClientResponseTraceEnricher2(IOptions<HttpClientTracingOptions> _)
    {
    }

    public void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        // nop
    }
}
#endif

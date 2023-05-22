// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public sealed class TestHttpTraceEnricher : IHttpTraceEnricher
{
    public bool IsEnrichCalled { get; set; }

    public TestHttpTraceEnricher(IOptions<HttpTracingOptions> _)
    {
    }

    public void Enrich(Activity activity, HttpRequest request)
    {
        IsEnrichCalled = true;
        Assert.NotNull(request);
    }
}

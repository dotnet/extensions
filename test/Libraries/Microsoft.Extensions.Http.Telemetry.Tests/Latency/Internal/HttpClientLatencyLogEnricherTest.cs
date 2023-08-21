// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Telemetry.Latency.Internal;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Latency;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Test.Internal;

public class HttpClientLatencyLogEnricherTest
{
    [Fact]
    public void HttpClientLatencyLogEnricher_NoOp_OnRequest()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, null);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void HttpClientLatencyLogEnricher_Enriches_OnResponseWithoutHeader()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using HttpResponseMessage httpResponseMessage = new();

        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();

        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, httpResponseMessage);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("latencyInfo")), It.Is<string>(s => s.Contains("a/b"))), Times.Once);
    }

    [Fact]
    public void HttpClientLatencyLogEnricher_Enriches_OnResponseWithHeader()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using HttpResponseMessage httpResponseMessage = new();
        string serverName = "serverNameVal";
        httpResponseMessage.Headers.Add(TelemetryConstants.ServerApplicationNameHeader, serverName);

        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();

        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, httpResponseMessage);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("latencyInfo")), It.Is<string>(s => s.Contains("a/b") && s.Contains(serverName))), Times.Once);
    }
}

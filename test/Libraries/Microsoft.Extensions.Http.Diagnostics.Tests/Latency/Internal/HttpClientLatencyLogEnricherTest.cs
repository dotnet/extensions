// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Latency.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Latency.Test.Internal;

public class HttpClientLatencyLogEnricherTest
{
    [Theory]
    [InlineData("a,b/c", "a_b_c")]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    public void HttpClientLatencyLogEnricher_EscapesDelimiters_InStringFields(string value, string expected)
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var mediator = new HttpLatencyMediator(lcti.Object);
        var tags = new ArraySegment<Tag>(new[] { new Tag($"tag{value}", $"value{value}"), new Tag("second", "other") });
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint($"checkpoint{value}", 100, 1000) });
        var measures = new ArraySegment<Measure>(new[] { new Measure($"measure{value}", 42) });
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(new LatencyData(tags, checkpoints, measures, default, default));
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using HttpResponseMessage response = new();
        response.Headers.Add(TelemetryConstants.ServerApplicationNameHeader, $"server{value}");
        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object, mediator);
        var collector = new Mock<IEnrichmentTagCollector>();
        string expectedValue = $"v1.0,server{expected},tag{expected}/second/,value{expected}/other/,"
            + $"checkpoint{expected}/,100/,measure{expected}/,42/";

        enricher.Enrich(collector.Object, null, response, null);

        collector.Verify(c => c.Add("LatencyInfo", expectedValue), Times.Once);
    }

    [Theory]
    [InlineData("server,a/b", "server_a_b")]
    [InlineData("server", "server")]
    public void HttpClientLatencyLogEnricher_EscapesDelimiters_WithoutLatencyContext(string serverName, string expected)
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var mediator = new HttpLatencyMediator(lcti.Object);
        var enricher = new HttpClientLatencyLogEnricher(new HttpClientLatencyContext(), lcti.Object, mediator);
        using HttpResponseMessage response = new();
        response.Headers.Add(TelemetryConstants.ServerApplicationNameHeader, serverName);
        var collector = new Mock<IEnrichmentTagCollector>();

        enricher.Enrich(collector.Object, null, response, null);

        collector.Verify(c => c.Add("LatencyInfo", $"v1.0,{expected},"), Times.Once);
    }

    [Fact]
    public void HttpClientLatencyLogEnricher_NoOp_OnRequest()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var mediator = new HttpLatencyMediator(lcti.Object);
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);
        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object, mediator);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, null, null);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void HttpClientLatencyLogEnricher_Enriches_OnResponseWithoutHeader()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var mediator = new HttpLatencyMediator(lcti.Object);
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using HttpResponseMessage httpResponseMessage = new();
        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object, mediator);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();

        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, httpResponseMessage, null);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("LatencyInfo")), It.Is<string>(s => s.Contains("a/b"))), Times.Once);
    }

    [Fact]
    public void HttpClientLatencyLogEnricher_Enriches_OnResponseWithHeader()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var mediator = new HttpLatencyMediator(lcti.Object);
        var checkpoints = new ArraySegment<Checkpoint>(new[] { new Checkpoint("a", default, default), new Checkpoint("b", default, default) });
        var ld = new LatencyData(default, checkpoints, default, default, default);
        var lc = HttpMockProvider.GetLatencyContext();
        lc.Setup(lc => lc.LatencyData).Returns(ld);
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using HttpResponseMessage httpResponseMessage = new();
        string serverName = "serverNameVal";
        httpResponseMessage.Headers.Add(TelemetryConstants.ServerApplicationNameHeader, serverName);

        var enricher = new HttpClientLatencyLogEnricher(context, lcti.Object, mediator);
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();

        enricher.Enrich(mockEnrichmentPropertyBag.Object, null!, httpResponseMessage, null);
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("LatencyInfo")), It.Is<string>(s => s.Contains("a/b") && s.Contains(serverName))), Times.Once);
    }
}

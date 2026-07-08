// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Diagnostics;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Latency.Test.Internal;

public class HttpLatencyLogEnricherTests
{
    [Fact]
    public void HttpLatencyLogEnricher_DoesNotEnrich_OnNullContext()
    {
        HttpContext? httpContext = null;
        var headers = new HeaderDictionary
        {
        };

        var request = new Mock<HttpRequest>();
        request.Setup(a => a.Headers).Returns(headers);
        var enricher = new HttpLatencyLogEnricher();
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        Assert.Throws<NullReferenceException>(() => enricher.Enrich(mockEnrichmentPropertyBag.Object, httpContext!));
        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => true), It.Is<string>(s => true)), Times.Never);
    }

    [Fact]
    public void HttpLatencyLogEnricher_DoesNotEnrich_WithoutLatencyContext()
    {
        var context = GetHttpContext(null!);
        var headers = new HeaderDictionary
        {
        };

        var request = new Mock<HttpRequest>();
        request.Setup(a => a.Headers).Returns(headers);
        var enricher = new HttpLatencyLogEnricher();
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        enricher.Enrich(mockEnrichmentPropertyBag.Object, context);

        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => true), It.Is<string>(s => true)), Times.Never);
    }

    [Fact]
    public void HttpLatencyLogEnricher_Enriches_OnRequestWithoutHeader()
    {
        var ld = new MockLatencyData();
        var lc = new Mock<ILatencyContext>();
        lc.Setup(a => a.LatencyData).Returns(ld.LatencyData);
        var context = GetHttpContext(lc.Object);
        var headers = new HeaderDictionary
        {
        };

        var request = new Mock<HttpRequest>();
        request.Setup(a => a.Headers).Returns(headers);
        var enricher = new HttpLatencyLogEnricher();
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        enricher.Enrich(mockEnrichmentPropertyBag.Object, context);

        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("latencyInfo", StringComparison.Ordinal)), It.Is<string>(s => s.Contains(ld.SerializedLatencyData)
        && s.Contains(HttpLatencyLogEnricher.DataVersion))), Times.Once);
    }

    [Fact]
    public void HttpLatencyLogEnricher_Enriches_OnResponseWithHeader()
    {
        var ld = new MockLatencyData();
        var lc = new Mock<ILatencyContext>();
        lc.Setup(a => a.LatencyData).Returns(ld.LatencyData);
        var headerName = "TestClient";
        var headers = new HeaderDictionary
        {
            { TelemetryConstants.ClientApplicationNameHeader, headerName }
        };

        var context = GetHttpContext(lc.Object, headers);
        var enricher = new HttpLatencyLogEnricher();
        Mock<IEnrichmentTagCollector> mockEnrichmentPropertyBag = new Mock<IEnrichmentTagCollector>();
        enricher.Enrich(mockEnrichmentPropertyBag.Object, context);

        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("latencyInfo", StringComparison.Ordinal)),
            It.Is<string>(s => s.Contains(ld.SerializedLatencyData) && s.Contains(headerName))), Times.Once);
    }

    private static HttpContext GetHttpContext(ILatencyContext latencyContext, IHeaderDictionary? headers = null)
    {
        var httpContextMock = new DefaultHttpContext();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                httpContextMock.Request.Headers[header.Key] = header.Value;
            }
        }

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(serviceProvider => serviceProvider.GetService(typeof(ILatencyContext)))
            .Returns(latencyContext);
        httpContextMock.RequestServices = serviceProviderMock.Object;
        return httpContextMock;
    }
}

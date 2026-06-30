// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
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

        mockEnrichmentPropertyBag.Verify(m => m.Add(It.Is<string>(s => s.Equals("latencyInfo", StringComparison.Ordinal)), It.Is<string>(s => s.Contains(ld.PerfPanelString)
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
            It.Is<string>(s => s.Contains(ld.PerfPanelString) && s.Contains(headerName))), Times.Once);
    }

    private class MockLatencyData
    {
        private readonly ArraySegment<Checkpoint> _checkpoints = new(new[]
        {
            new Checkpoint("ca", 1, 1000),
            new Checkpoint("cb", 2, 1000),
            new Checkpoint("c/c", 3, 1000)
        });

        private readonly ArraySegment<Measure> _measures = new(new[]
        {
            new Measure("m/a", 1),
            new Measure("mb", 2),
            new Measure("mc", 3),
        });

        private readonly ArraySegment<Tag> _tags = new(new[]
        {
            new Tag("t/a", "t1"),
            new Tag("tb", "t/2"),
            new Tag("tc", "t3")
        });

        public MockLatencyData()
        {
            const int MillisecondsPerSecond = 1000;

            LatencyData = new LatencyData(_tags, _checkpoints, _measures, 20, 1000);

            PerfPanelString = string.Format(CultureInfo.InvariantCulture, "{0}/,{1}/,{2}/,{3}/,{4}/,{5}/,{6}",
                string.Join("/", _tags.Select(a => a.Name.Replace('/', '_'))),
                string.Join("/", _tags.Select(a => a.Value.Replace('/', '_'))),
                string.Join("/", _checkpoints.Select(a => a.Name.Replace('/', '_'))),
                string.Join("/", _checkpoints.Select(a => (long)Math.Round(((double)a.Elapsed / a.Frequency) * MillisecondsPerSecond))),
                string.Join("/", _measures.Select(a => a.Name.Replace('/', '_'))),
                string.Join("/", _measures.Select(a => a.Value)),
                (long)Math.Round(((double)LatencyData.DurationTimestamp / LatencyData.DurationTimestampFrequency) * MillisecondsPerSecond));
        }

        public string PerfPanelString { get; private set; }

        public LatencyData LatencyData { get; private set; }
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

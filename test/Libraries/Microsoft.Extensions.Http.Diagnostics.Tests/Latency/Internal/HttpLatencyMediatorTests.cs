// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Latency.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.Http.Latency.Test.Internal;

#if NET
public class HttpLatencyMediatorTests
{
    [Fact]
    public void RecordStart_RecordsGCPauseMeasure()
    {
        // Arrange
        var lcti = HttpMockProvider.GetTokenIssuer();
        var measureToken = new MeasureToken(HttpMeasures.GCPauseTime, 0);
        lcti.Setup(i => i.GetMeasureToken(HttpMeasures.GCPauseTime))
            .Returns(measureToken);

        var lc = HttpMockProvider.GetLatencyContext();
        var mediator = new HttpLatencyMediator(lcti.Object);

        // Act
        mediator.RecordStart(lc.Object);

        // Assert
        // Verify RecordMeasure was called with the correct token
        lc.Verify(c => c.RecordMeasure(
                measureToken,
                It.Is<long>(v => v <= 0)), // Value should be negative (start value)
            Times.Once);
    }

    [Fact]
    public async Task HttpLatencyTelemetryHandler_UsesMediator()
    {
        // Arrange
        var lc = HttpMockProvider.GetLatencyContext();
        var lcp = HttpMockProvider.GetContextProvider(lc);
        lcp.Setup(p => p.CreateContext()).Returns(lc.Object);

        var context = new HttpClientLatencyContext();

        var sop = new Mock<IOptions<ApplicationMetadata>>();
        sop.Setup(a => a.Value).Returns(new ApplicationMetadata());
        var hop = new Mock<IOptions<HttpClientLatencyTelemetryOptions>>();
        hop.Setup(a => a.Value).Returns(new HttpClientLatencyTelemetryOptions());

        var lcti = HttpMockProvider.GetTokenIssuer();

        // Create a mediator
        var mediator = new HttpLatencyMediator(lcti.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        using var req = new HttpRequestMessage();
        req.Method = HttpMethod.Post;
        req.RequestUri = new Uri($"https://default-uri.com/foo");

        var resp = new HttpResponseMessage();
        var mockHandler = new Mock<DelegatingHandler>();
        mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(resp);

        using var handler = new HttpLatencyTelemetryHandler(
            listener, lcti.Object, lcp.Object, hop.Object, sop.Object, mediator);
        handler.InnerHandler = mockHandler.Object;
        // Act
        using var client = new HttpClient(handler);
        await client.SendAsync(req, It.IsAny<CancellationToken>());

        // Verify that the latency context was created and properly used
        lcp.Verify(p => p.CreateContext(), Times.Once);
    }

    [Fact]
    public void RecordEnd_RecordsGCPauseMeasure()
    {
        // Arrange
        var lcti = HttpMockProvider.GetTokenIssuer();
        var measureToken = new MeasureToken(HttpMeasures.GCPauseTime, 0);
        lcti.Setup(i => i.GetMeasureToken(HttpMeasures.GCPauseTime))
            .Returns(measureToken);

        var lc = HttpMockProvider.GetLatencyContext();
        var mediator = new HttpLatencyMediator(lcti.Object);

        // Act
        mediator.RecordEnd(lc.Object);

        lc.Verify(c => c.AddMeasure(measureToken, It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public void RecordEnd_WithResponse_SetsHttpVersionTag()
    {
        // Arrange
        var lcti = HttpMockProvider.GetTokenIssuer();
        var httpVersionToken = new TagToken(HttpTags.HttpVersion, 0);
        lcti.Setup(i => i.GetTagToken(HttpTags.HttpVersion))
            .Returns(httpVersionToken);

        var lc = HttpMockProvider.GetLatencyContext();
        var mediator = new HttpLatencyMediator(lcti.Object);

        using var response = new HttpResponseMessage();
        response.Version = new Version(2, 0);

        // Act
        mediator.RecordEnd(lc.Object, response);

        // Assert
        lc.Verify(c => c.SetTag(
                httpVersionToken,
                "2.0"),
            Times.Once);
    }

    [Fact]
    public void RecordEnd_WithNullResponse_DoesNotSetHttpVersionTag()
    {
        // Arrange
        var lcti = HttpMockProvider.GetTokenIssuer();
        var httpVersionToken = new TagToken("Http.Version", 0);
        lcti.Setup(i => i.GetTagToken(HttpTags.HttpVersion))
            .Returns(httpVersionToken);

        var lc = HttpMockProvider.GetLatencyContext();
        var mediator = new HttpLatencyMediator(lcti.Object);

        // Act
        mediator.RecordEnd(lc.Object);

        // Assert
        lc.Verify(c => c.SetTag(
                It.Is<TagToken>(t => t.Name == HttpTags.HttpVersion),
                It.IsAny<string>()),
            Times.Never);
    }
}
#endif
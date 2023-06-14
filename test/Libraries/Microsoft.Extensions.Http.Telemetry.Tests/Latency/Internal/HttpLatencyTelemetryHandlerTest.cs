// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Http.Telemetry.Latency.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Test.Internal;

public class HttpLatencyTelemetryHandlerTest
{
    [Fact]
    public void HttpLatencyTelemetryHandler_InvokesTokenIssuer()
    {
        var lc = HttpMockProvider.GetLatencyContext();
        var lcp = HttpMockProvider.GetContextProvider(lc);
        var context = new HttpClientLatencyContext();
        var sop = new Mock<IOptions<ApplicationMetadata>>();
        sop.Setup(a => a.Value).Returns(new ApplicationMetadata());
        var hop = new Mock<IOptions<HttpClientLatencyTelemetryOptions>>();
        hop.Setup(a => a.Value).Returns(new HttpClientLatencyTelemetryOptions());

        var lcti = HttpMockProvider.GetTokenIssuer();
        var lcti2 = HttpMockProvider.GetTokenIssuer();

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        using var handler = new HttpLatencyTelemetryHandler(listener, lcti2.Object, lcp.Object, hop.Object, sop.Object);

        lcti2.Verify(a => a.GetCheckpointToken(It.Is<string>(s => !HttpCheckpoints.Checkpoints.Contains(s))), Times.Never);
        lcti2.Verify(a => a.GetCheckpointToken(It.Is<string>(s => HttpCheckpoints.Checkpoints.Contains(s))));
    }

    [Fact]
    public async Task HttpLatencyTelemetryHandler_SetsLatencyContext()
    {
        var lc = HttpMockProvider.GetLatencyContext();
        var lcp = HttpMockProvider.GetContextProvider(lc);
        var context = new HttpClientLatencyContext();
        var sop = new Mock<IOptions<ApplicationMetadata>>();
        sop.Setup(a => a.Value).Returns(new ApplicationMetadata());
        var hop = new Mock<IOptions<HttpClientLatencyTelemetryOptions>>();
        hop.Setup(a => a.Value).Returns(new HttpClientLatencyTelemetryOptions());

        var lcti = HttpMockProvider.GetTokenIssuer();
        var lcti2 = HttpMockProvider.GetTokenIssuer();

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        using var req = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo")
        };

        var resp = new Mock<HttpResponseMessage>();
        var mockHandler = new Mock<DelegatingHandler>();
        mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()).Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                Assert.NotNull(context.Get());
                Assert.True(req.Headers.Contains(TelemetryConstants.ClientApplicationNameHeader));
            }).Returns(Task.FromResult(resp.Object));

        using var handler = new HttpLatencyTelemetryHandler(listener, lcti2.Object, lcp.Object, hop.Object, sop.Object)
        {
            InnerHandler = mockHandler.Object
        };

        using var client = new System.Net.Http.HttpClient(handler);
        await client.SendAsync(req, It.IsAny<CancellationToken>()).ConfigureAwait(false);
        Assert.Null(context.Get());
    }

    [Fact]
    public void HttpLatencyTelemetryHandler_IfDetailsDisabled_DoesNotEnableListener()
    {
        var lc = HttpMockProvider.GetLatencyContext();
        var lcp = HttpMockProvider.GetContextProvider(lc);
        var context = new HttpClientLatencyContext();
        var sop = new Mock<IOptions<ApplicationMetadata>>();
        sop.Setup(a => a.Value).Returns(new ApplicationMetadata());
        var hop = new Mock<IOptions<HttpClientLatencyTelemetryOptions>>();
        hop.Setup(a => a.Value).Returns(new HttpClientLatencyTelemetryOptions { EnableDetailedLatencyBreakdown = false });
        var lcti = HttpMockProvider.GetTokenIssuer();

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        using var handler = new HttpLatencyTelemetryHandler(listener, lcti.Object, lcp.Object, hop.Object, sop.Object);
        Assert.False(listener.Enabled);
    }
}

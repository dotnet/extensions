// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Latency.Internal;
using Moq;

namespace Microsoft.Extensions.Http.Latency.Test.Internal;

internal static class HttpMockProvider
{
    public static HttpRequestLatencyListener GetListener(HttpClientLatencyContext httpClientLatencyContext, ILatencyContextTokenIssuer tokenIssuer)
    {
        HttpRequestLatencyListener hrll = new HttpRequestLatencyListener(httpClientLatencyContext, tokenIssuer);
        return hrll;
    }

    public static Mock<ILatencyContextTokenIssuer> GetTokenIssuer()
    {
        var lcti = new Mock<ILatencyContextTokenIssuer>();
        lcti.Setup(a => a.GetCheckpointToken(It.IsAny<string>()))
            .Returns((string c) => { return new CheckpointToken(c, 0); });
        return lcti;
    }

    public static Mock<ILatencyContextProvider> GetContextProvider(Mock<ILatencyContext> lc)
    {
        var lcp = new Mock<ILatencyContextProvider>();
        lcp.Setup(a => a.CreateContext()).Returns(lc.Object);

        return lcp;
    }

    public static Mock<ILatencyContext> GetLatencyContext()
    {
        var lc = new Mock<ILatencyContext>();
        lc.Setup(a => a.AddCheckpoint(It.IsAny<CheckpointToken>()));
        return lc;
    }

    public class MockEventSource : EventSource
    {
        public int OnEventInvoked;

        protected override void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command)
        {
            Interlocked.Increment(ref OnEventInvoked);
        }
    }

    public class HttpMockEventSource : MockEventSource
    {
    }

    public class SockeyMockEventSource : MockEventSource
    {
    }

    public class NameResolutionEventSource : MockEventSource
    {
    }
}

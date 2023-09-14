// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Http.Telemetry.Latency.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Test.Internal;

public class HttpRequestLatencyListenerTest
{
    [Fact]
    public void HttpClientLatencyContext_Set_BasicFunction()
    {
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);
        Assert.Equal(context.Get(), lc.Object);
        context.Unset();
        Assert.Null(context.Get());
    }

    [Fact]
    public void HttpRequestLatencyListener_InvokesTokenIssuer()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        Assert.NotNull(listener);

        lcti.Verify(a => a.GetCheckpointToken(It.Is<string>(s => !HttpCheckpoints.Checkpoints.Contains(s))), Times.Never);
        lcti.Verify(a => a.GetCheckpointToken(It.Is<string>(s => HttpCheckpoints.Checkpoints.Contains(s))));
    }

    [Fact]
    public void HttpRequestLatencyListener_OnDisabled_DoesNotEnableEventSource()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        Assert.NotNull(listener);

        using var es = new HttpMockProvider.MockEventSource();
        listener.OnEventSourceCreated("test", es);
        Assert.Equal(0, es.OnEventInvoked);
        Assert.False(es.IsEnabled());

        using var esSockets = new HttpMockProvider.SockeyMockEventSource();
        listener.OnEventSourceCreated("System.Net.Sockets", esSockets);
        Assert.Equal(0, esSockets.OnEventInvoked);
        Assert.False(esSockets.IsEnabled());

        using var esHttp = new HttpMockProvider.HttpMockEventSource();
        listener.OnEventSourceCreated("System.Net.Http", esHttp);
        Assert.Equal(0, esHttp.OnEventInvoked);
        Assert.False(esHttp.IsEnabled());

        using var esNameRes = new HttpMockProvider.NameResolutionEventSource();
        listener.OnEventSourceCreated("System.Net.NameResolution", esNameRes);
        Assert.Equal(0, esNameRes.OnEventInvoked);
        Assert.False(esNameRes.IsEnabled());
    }

    [Fact]
    public void HttpRequestLatencyListener_OnEventSourceCreated_NonHttpSources()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        Assert.NotNull(listener);
        listener.Enable();

        using var es = new HttpMockProvider.MockEventSource();
        listener.OnEventSourceCreated("test", es);
        Assert.Equal(0, es.OnEventInvoked);
        Assert.False(es.IsEnabled());
    }

    [Fact]
    public void HttpRequestLatencyListener_OnEventSourceCreated_HttpSources()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        Assert.NotNull(listener);
        listener.Enable();

        using var esSockets = new HttpMockProvider.SockeyMockEventSource();
        listener.OnEventSourceCreated("System.Net.Sockets", esSockets);
        Assert.Equal(1, esSockets.OnEventInvoked);
        Assert.True(esSockets.IsEnabled());

        using var esHttp = new HttpMockProvider.HttpMockEventSource();
        listener.OnEventSourceCreated("System.Net.Http", esHttp);
        Assert.Equal(1, esHttp.OnEventInvoked);
        Assert.True(esHttp.IsEnabled());

        using var esNameRes = new HttpMockProvider.NameResolutionEventSource();
        listener.OnEventSourceCreated("System.Net.NameResolution", esNameRes);
        Assert.Equal(1, esNameRes.OnEventInvoked);
        Assert.True(esNameRes.IsEnabled());
    }

    [Fact]
    public void HttpRequestLatencyListener_OnEventSourceCreated_Twice()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);
        Assert.NotNull(listener);
        listener.Enable();

        using var esSockets = new HttpMockProvider.SockeyMockEventSource();
        listener.OnEventSourceCreated("System.Net.Sockets", esSockets);
        Assert.Equal(1, esSockets.OnEventInvoked);
        Assert.True(esSockets.IsEnabled());

        listener.OnEventSourceCreated("System.Net.Sockets", esSockets);
        Assert.Equal(1, esSockets.OnEventInvoked);
        Assert.True(esSockets.IsEnabled());
    }

    [Fact]
    public void HttpRequestLatencyListener_OnEventWritten_DoesNotAddCheckpoints_NonHttp()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);

        var events = new[]
        {
            "ConnectionEstablished", "RequestLeftQueue", "ResolutionStop", "ConnectStart", "New"
        };

        for (int i = 0; i < events.Length; i++)
        {
            listener.OnEventWritten("System.Net", events[i]);
        }

        lc.Verify(a => a.AddCheckpoint(It.IsAny<CheckpointToken>()), Times.Never);
    }

    [Fact]
    public void HttpRequestLatencyListener_OnEventWritten_AddsCheckpoints_Http()
    {
        var lcti = HttpMockProvider.GetTokenIssuer();
        var lc = HttpMockProvider.GetLatencyContext();
        var context = new HttpClientLatencyContext();
        context.Set(lc.Object);

        using var listener = HttpMockProvider.GetListener(context, lcti.Object);

        var httpEvents = new[]
        {
            "ConnectionEstablished", "RequestLeftQueue", "RequestContentStart", "RequestContentStop",
            "ResponseHeadersStart", "ResponseHeadersStop", "ResponseContentStart", "ResponseContentStop"
        };
        int numHttpEvents = httpEvents.Length;

        for (int i = 0; i < numHttpEvents; i++)
        {
            listener.OnEventWritten("System.Net.Http", httpEvents[i]);
        }

        lc.Verify(a => a.AddCheckpoint(It.IsAny<CheckpointToken>()), Times.Exactly(numHttpEvents));

        var socketEvents = new[]
        {
            "ConnectStart", "ConnectStop"
        };
        int numSocketEvents = socketEvents.Length;

        for (int i = 0; i < numSocketEvents; i++)
        {
            listener.OnEventWritten("System.Net.Sockets", socketEvents[i]);
        }

        lc.Verify(a => a.AddCheckpoint(It.IsAny<CheckpointToken>()), Times.Exactly(numHttpEvents + numSocketEvents));

        var dnsEvents = new[]
        {
            "ResolutionStart", "ResolutionStop"
        };
        int numDnsEvents = dnsEvents.Length;

        for (int i = 0; i < numDnsEvents; i++)
        {
            listener.OnEventWritten("System.Net.NameResolution", dnsEvents[i]);
        }

        lc.Verify(a => a.AddCheckpoint(It.IsAny<CheckpointToken>()),
            Times.Exactly(numHttpEvents + numSocketEvents + numDnsEvents));
    }
}

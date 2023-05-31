// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;
using Microsoft.Extensions.Telemetry;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test;

#pragma warning disable CS0618 // remove when IHttpClientTraceEnricher.Enrich(Activity, httpRequestMessage) API is deprecated.
public class HttpClientTraceEnrichmentProcessorTests
{
    [Fact]
    public void HttpClientTraceEnrichmentProcessor_NoEnrichers_DoesNotThrow()
    {
        using var host = FakeHost.CreateBuilder(options => options.ValidateOnBuild = false)
            .ConfigureWebHost(webBuilder => webBuilder
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddOpenTelemetry().WithTracing(builder => builder.AddHttpClientTracing())))
            .Build();

        var traceEnrichmentProcessor = host.Services.GetRequiredService<HttpClientTraceEnrichmentProcessor>();
        Assert.NotNull(traceEnrichmentProcessor);
    }

    [Fact]
    public void HttpClientTraceEnrichmentProcessor_WithHttpRequestMessage_MultipleEnrichers()
    {
        Mock<IHttpClientTraceEnricher> mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        Mock<IHttpClientTraceEnricher> mockTraceEnricher2 = new Mock<IHttpClientTraceEnricher>();

        using var host = FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services
                            .AddRouting()
                            .AddOpenTelemetry().WithTracing(builder => builder
                                .AddHttpClientTracing()
                                .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                                .AddHttpClientTraceEnricher(mockTraceEnricher2.Object)
                                .AddHttpClientTraceEnricher<TestHttpClientTraceEnricher>())))
            .Build();

        var traceEnrichmentProcessor = host.Services.GetRequiredService<HttpClientTraceEnrichmentProcessor>();
        Assert.NotNull(traceEnrichmentProcessor);

        string uriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(uriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        activity.SetRequest(httpRequestMessage);
        traceEnrichmentProcessor.OnEnd(activity);

        mockTraceEnricher1.Verify(m => m.Enrich(activity, httpRequestMessage, null), Times.Once);
        mockTraceEnricher2.Verify(m => m.Enrich(activity, httpRequestMessage, null), Times.Once);

        Assert.NotNull(activity.GetRequest());
    }

    [Fact]
    public void HttpClientTraceEnrichmentProcessor_WithHttpResponseMessage()
    {
        Mock<IHttpClientTraceEnricher> mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        using var host = FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services
                            .AddRouting()
                            .AddOpenTelemetry().WithTracing(builder => builder
                                .AddHttpClientTracing()
                                .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                                .AddHttpClientTraceEnricher<TestHttpClientResponseTraceEnricher>())))
            .Build();

        var traceEnrichmentProcessor = host.Services.GetRequiredService<HttpClientTraceEnrichmentProcessor>();
        Assert.NotNull(traceEnrichmentProcessor);

        using Activity activity = new Activity("test");
        using var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        activity.SetResponse(httpResponseMessage);
        traceEnrichmentProcessor.OnEnd(activity);

        mockTraceEnricher1.Verify(m => m.Enrich(activity, null, httpResponseMessage), Times.Once);

        Assert.Null(activity.GetResponse());
    }

    [Fact]
    public async Task HttpClientTraceEnrichmentProcessor_NoHTTPRequest_DoesNotCallEnrich()
    {
        using ActivitySource activitySource = new ActivitySource(nameof(HttpClientTraceEnrichmentProcessor_NoHTTPRequest_DoesNotCallEnrich));
        using var testServer = TestHttpServer.RunServerOrThrow(ctx => ctx.Response.OutputStream.Close(), out var hostName, out var port);
        var serverHost = $"{hostName}:{port}";

        var mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(nameof(HttpClientTraceEnrichmentProcessor_NoHTTPRequest_DoesNotCallEnrich))
                    .AddHttpClientTracing()
                    .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)))
            .StartAsync();

        using var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();

        mockTraceEnricher1.Verify(m => m.Enrich(activity!, It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Never);
    }
}

#endif

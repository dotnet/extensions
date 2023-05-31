// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;
using Microsoft.Extensions.Telemetry;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test;

public class HttpClientTraceEnrichmentProcessorTests
{
    [Fact]
    public void HttpClientTraceEnrichmentProcessor_NoEnrichers_DoesNotThrow()
    {
        using var host = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddLogging()
                .AddRedaction()
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(_ => { })
                .UseMvc())
            .Build();

        var traceEnrichmentProcessor = host.Services.GetRequiredService<HttpClientTraceEnrichmentProcessor>();
        Assert.NotNull(traceEnrichmentProcessor);
    }

    [Fact]
    public void HttpClientTraceEnrichmentProcessor_WithRequestMetadata_MultipleEnrichers()
    {
        Mock<IHttpClientTraceEnricher> mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        Mock<IHttpClientTraceEnricher> mockTraceEnricher2 = new Mock<IHttpClientTraceEnricher>();

        using var host = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()
                    .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                    .AddHttpClientTraceEnricher(mockTraceEnricher2.Object)
                    .AddHttpClientTraceEnricher<TestHttpClientTraceEnricher>()))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(_ => { })
                .UseMvc())
            .Build();

        var traceEnrichmentProcessor = host.Services.GetRequiredService<HttpClientTraceEnrichmentProcessor>();
        Assert.NotNull(traceEnrichmentProcessor);

        string uriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        using Activity activity = new Activity("test");
        var request = WebRequest.CreateHttp(uriString);
        Assert.NotNull(request);
        request.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });
        using var response = new FakeHttpWebResponse(uriString);

        activity.SetRequest(request);
        activity.SetResponse(response);
        traceEnrichmentProcessor.OnEnd(activity);

        mockTraceEnricher1.Verify(m => m.Enrich(activity, request, response), Times.Once);
        mockTraceEnricher2.Verify(m => m.Enrich(activity, request, response), Times.Once);

        Assert.NotNull(activity.GetRequest());
        Assert.Null(activity.GetResponse());
    }
}

#endif

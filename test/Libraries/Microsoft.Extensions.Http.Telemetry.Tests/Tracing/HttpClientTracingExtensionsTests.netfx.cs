// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;
using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test;

public class HttpClientTracingExtensionsTests
{
    private static readonly string _httpPrefix = Uri.UriSchemeHttp + Uri.SchemeDelimiter;

    [Fact]
    public void AddHttpClientTracing_GivenNullArgument_Throws()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpClientTracingOptions");

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTracing());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTracing(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTracing(configSection));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddHttpClientTracing((Action<HttpClientTracingOptions>)null!)));

        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddHttpClientTracing((IConfigurationSection)null!)));
    }

    [Fact]
    public void AddHttpClientTraceEnricher_GivenNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTraceEnricher<TestHttpClientTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTraceEnricher(
                new TestHttpClientTraceEnricher(MSOptions.Options.Create(new HttpClientTracingOptions()))));
    }

    [Theory]
    [CombinatorialData]
    public void AddHttpClientTracing_WithConfigSection(bool isLoggerPresent)
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpClientTracingOptions");

        Mock<IHttpClientTraceEnricher> mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        Mock<IHttpClientTraceEnricher> mockTraceEnricher2 = new Mock<IHttpClientTraceEnricher>();

        var hostBuilder = new WebHostBuilder();

        if (isLoggerPresent)
        {
            hostBuilder.ConfigureLogging(builder => builder.AddFakeLogging());
        }

        var host = hostBuilder
            .ConfigureServices(services => services
                .AddRouting()
                .AddRedaction()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing(configSection)
                    .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                    .AddHttpClientTraceEnricher(mockTraceEnricher2.Object)))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(_ => { })
                .UseMvc())
            .Build();

        var enrichers = host.Services.GetServices<IHttpClientTraceEnricher>();
        Assert.Equal(2, enrichers.Count());

        var processor = host.Services.GetService<HttpClientRedactionProcessor>();
        Assert.NotNull(processor);

        var logger = host.Services.GetService<ILogger<HttpClientRedactionProcessor>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public async Task AddHttpClientTracing_WithRequestMetadataAndRedaction_WorksCorrectly()
    {
        const string Domain = "hopefully-no-such-domain";
        const string UriString = $"https://{Domain}/api/routes/routeId123/chats/chatId123";

        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing(options =>
                    {
                        options
                            .RouteParameterDataClasses
                            .Add("chatId", SimpleClassifications.PrivateData);

                        options
                            .RouteParameterDataClasses
                            .Add("routeId", SimpleClassifications.PrivateData);
                    })
                    .AddTestTraceProcessor(traceProcessor)))
            .StartAsync();

        var request = WebRequest.CreateHttp(UriString);
        request.Method = "GET";
        request.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "api/routes/{routeId}/chats/{chatId}"
        });

        try
        {
            await request.GetResponseAsync();
        }
        catch (WebException)
        {
            // no op
        }

        var activity = traceProcessor.FirstActivity!;

        Assert.NotNull(activity);
        Assert.Equal("api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.DisplayName);
        Assert.Equal($"https://{Domain}/api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal(typeof(WebException).FullName, activity.GetTagItem(Constants.AttributeExceptionType));
        Assert.Contains(Domain, (string?)activity.GetTagItem(Constants.AttributeExceptionMessage));
        activity.AssertSensitiveTagsAreNull();
    }

    [Fact]
    public async Task AddHttpClientTracing_WithEnrichment_WorksCorrectly()
    {
        using var testServer = TestHttpServer.RunServerOrThrow(ctx => ctx.Response.OutputStream.Close(), out var hostName, out var port);
        var serverHost = $"{hostName}:{port}";

        var mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        var mockTraceEnricher2 = new Mock<IHttpClientTraceEnricher>();

        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()
                    .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                    .AddHttpClientTraceEnricher(mockTraceEnricher2.Object)
                    .AddTestTraceProcessor(traceProcessor)))
            .StartAsync();
        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();
        using var httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(_httpPrefix + serverHost);

        await httpClient.SendAsync(httpRequestMessage);

        var activity = traceProcessor.FirstActivity!;

        mockTraceEnricher1.Verify(m => m.Enrich(activity, It.IsNotNull<HttpWebRequest>(), It.IsNotNull<HttpWebResponse>()), Times.Once);
        mockTraceEnricher2.Verify(m => m.Enrich(activity, It.IsNotNull<HttpWebRequest>(), It.IsNotNull<HttpWebResponse>()), Times.Once);
    }

    [Fact]
    public async Task AddHttpClientTracing_WithException_CallsEnrichOnce()
    {
        var mockTraceEnricher = new Mock<IHttpClientTraceEnricher>();
        const string UriString = "https://hopefully-no-such-domain/";
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()
                    .AddHttpClientTraceEnricher(mockTraceEnricher.Object)))
            .StartAsync();

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, UriString);

        try
        {
            await httpClient.SendAsync(httpRequestMessage);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        mockTraceEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpWebRequest>(), It.IsAny<HttpWebResponse>()), Times.Once);
    }

    [Fact]
    public void AddHttpClientTracing_WhenRedactionIsNotRegistered_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new WebHostBuilder()
            .ConfigureServices(services => services
                .AddRouting()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing(options => options.RouteParameterDataClasses.Add("CCC", SimpleClassifications.PrivateData))))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(_ => { })
                .UseMvc())
            .Build()
            .Services
            .GetRequiredService<HttpClientRedactionProcessor>());
    }

    [Fact]
    public async Task AddHttpClientTracing_GetRequestMetadataFromOutgoingRequestContext_ActivityEnrichedWithMetadata()
    {
        const string UriString = "https://hopefully-no-such-domain/api/test/url/1";

        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()
                    .AddTestTraceProcessor(traceProcessor)))
            .StartAsync();

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, UriString);

        var requestContext = host.Services.GetRequiredService<IOutgoingRequestContext>();
        RequestMetadata requestMetadata = new()
        {
            RequestName = "TestUrl",
            RequestRoute = "api/test/url/{id}"
        };
        requestContext.SetRequestMetadata(requestMetadata);

        try
        {
            await httpClient.SendAsync(httpRequestMessage);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        var activity = traceProcessor.FirstActivity!;
        Assert.Equal(requestMetadata.RequestName, activity.DisplayName);
        Assert.Equal($"https://hopefully-no-such-domain/api/test/url/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
        activity.AssertSensitiveTagsAreNull();
    }

    [Fact]
    public async Task AddHttpClientTracing_GetRequestMetadataFromDownstreamDependencyMetadataManager_ActivityEnrichedWithMetadata()
    {
        const string UriString = "https://hopefully-no-such-domain/api/test/url/1";

        Mock<IDownstreamDependencyMetadataManager> downstreamDependencyMetadataManager = new();
        RequestMetadata requestMetadata = new()
        {
            RequestName = "TestUrl",
            RequestRoute = "api/test/url/{id}"
        };

        downstreamDependencyMetadataManager
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpWebRequest>()))
            .Returns(requestMetadata)
            .Verifiable();

        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton(downstreamDependencyMetadataManager.Object)
                .AddHttpClient()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()
                    .AddTestTraceProcessor(traceProcessor)))
            .StartAsync();

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, UriString);

        try
        {
            await httpClient.SendAsync(httpRequestMessage);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        var activity = traceProcessor.FirstActivity!;
        Assert.Equal(requestMetadata.RequestName, activity.DisplayName);
        Assert.Equal($"https://hopefully-no-such-domain/api/test/url/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
        activity.AssertSensitiveTagsAreNull();

        downstreamDependencyMetadataManager.Verify();
    }
}

#endif

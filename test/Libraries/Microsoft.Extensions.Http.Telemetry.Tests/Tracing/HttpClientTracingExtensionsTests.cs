// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.TestUtilities;
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
    public void AddHttpClientTracing_WithNullArgument_Throws()
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
    public void AddHttpClientTraceEnricher_WithNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTraceEnricher<TestHttpClientTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpClientTraceEnricher(
                new TestHttpClientTraceEnricher(MSOptions.Options.Create(new HttpClientTracingOptions()))));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddHttpClientTraceEnricher<TestHttpClientTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddHttpClientTraceEnricher(
                new TestHttpClientTraceEnricher(MSOptions.Options.Create(new HttpClientTracingOptions()))));
    }

    [Fact]
    public async Task AddHttpClientTracing_WhenNoCustomHttpPathRedactor_RegistersDefaultHttpPathRedactor()
    {
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()))
            .StartAsync();

        var defaultRedactor = host.Services.GetService<IHttpPathRedactor>();
        Assert.NotNull(defaultRedactor);
        Assert.IsType<HttpPathRedactor>(defaultRedactor);
    }

    [Fact]
    public async Task AddHttpClientTracing_WithCustomHttpPathRedactorBeforeDefault_RegistersCustomHttpPathRedactor()
    {
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IHttpPathRedactor, TestHttpPathRedactor>()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()))
            .StartAsync();

        var customRedactor = host.Services.GetService<IHttpPathRedactor>();
        Assert.NotNull(customRedactor);
        Assert.IsType<TestHttpPathRedactor>(customRedactor);
    }

    [Fact]
    public async Task AddHttpClientTracing_WithCustomHttpPathRedactorAfterDefault_RegistersCustomHttpPathRedactor()
    {
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpClientTracing()).Services
                .AddSingleton<IHttpPathRedactor, TestHttpPathRedactor>())
            .StartAsync();

        var customRedactor = host.Services.GetService<IHttpPathRedactor>();
        Assert.NotNull(customRedactor);
        Assert.IsType<TestHttpPathRedactor>(customRedactor);
    }

    [Theory]
    [CombinatorialData]
    public void AddHttpClientTracing_WithConfigSection(bool isLoggerPresent)
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpClientTracingOptions");

        var mockTraceEnricher1 = new Mock<IHttpClientTraceEnricher>();
        var mockTraceEnricher2 = new Mock<IHttpClientTraceEnricher>();

        using var host = FakeHost.CreateBuilder(options => options.FakeRedaction = false)
            .ConfigureWebHost(webBuilder =>
            {
                if (isLoggerPresent)
                {
                    webBuilder.ConfigureLogging(builder => builder.AddFakeLogging());
                }

                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services
                        .AddRouting()
                        .AddRedaction()
                        .AddOpenTelemetry().WithTracing(builder => builder
                            .AddHttpClientTracing(configSection)
                            .AddHttpClientTraceEnricher(mockTraceEnricher1.Object)
                            .AddHttpClientTraceEnricher(mockTraceEnricher2.Object)));
            })
            .Build();

        var enrichers = host.Services.GetServices<IHttpClientTraceEnricher>();
        Assert.Equal(2, enrichers.Count());

        var processor = host.Services.GetService<HttpClientRedactionProcessor>();
        Assert.NotNull(processor);

        var logger = host.Services.GetService<ILogger<HttpClientRedactionProcessor>>();
        Assert.NotNull(logger);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    public async Task AddHttpClientTracing_WebRequestSetInCustomActivity_Works()
    {
        const string UriString = "https://hopefully-no-such-domain/api/routes/routeId123/chats/chatId123";
        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddHttpClient()
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

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, UriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "api/routes/{routeId}/chats/{chatId}"
        });
        try
        {
            await httpClient.SendAsync(httpRequestMessage);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        var activity = traceProcessor.FirstActivity!;
        Assert.Equal("api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.DisplayName);
        Assert.Equal("https://hopefully-no-such-domain/api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal(typeof(HttpRequestException).FullName, activity.GetTagItem(Constants.AttributeExceptionType));
        Assert.Contains("No such host is known", (string?)activity.GetTagItem(Constants.AttributeExceptionMessage));
        activity.AssertSensitiveTagsAreNull();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
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

        try
        {
            await httpClient.GetAsync(UriString);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        mockTraceEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Once);
    }

    [Fact]
    public void UrlRedactionProcessor_WhenNoRedactorProvider_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FakeHost.CreateBuilder(new FakeHostOptions { FakeRedaction = false, ValidateOnBuild = false })
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services
                        .AddRouting()
                        .AddHttpClient()
                        .AddOpenTelemetry().WithTracing(builder => builder
                            .AddHttpClientTracing(options => options.RouteParameterDataClasses.Add("FWEJFNIWJ", SimpleClassifications.PrivateData))))
                    .Configure(_ => { }))
                .Build()
                .Services
                .GetRequiredService<HttpClientRedactionProcessor>());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    public async Task AddHttpClientTracing_SetRequestMetadataOnOutgoingRequestContext_ActivityEnrichedWithMetadata()
    {
        const string UriString = "https://hopefully-no-such-domain/api/routes/routeId123/chats/chatId123";
        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddHttpClient()
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

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, UriString);

        var requestContext = host.Services.GetRequiredService<IOutgoingRequestContext>();
        requestContext.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "api/routes/{routeId}/chats/{chatId}"
        });

        try
        {
            await httpClient.SendAsync(httpRequestMessage);
        }
        catch (HttpRequestException)
        {
            // no op
        }

        var activity = traceProcessor.FirstActivity!;
        Assert.Equal("api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.DisplayName);
        Assert.Equal("https://hopefully-no-such-domain/api/routes/RedactedData:routeId123/chats/RedactedData:chatId123", activity.GetTagItem(Constants.AttributeHttpUrl));
        activity.AssertSensitiveTagsAreNull();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux, SkipReason = "See https://github.com/dotnet/r9/issues/289")]
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
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
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

    [Theory(Skip = "Flaky")]
    [InlineData(HttpRouteParameterRedactionMode.None, "api/sensitive_id")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "api/sensitive_id")]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"api/{TelemetryConstants.Redacted}")]
    public async Task AddHttpClientTracing_WhenRequestIsCancelled_SensitiveDataRedacted(
        HttpRouteParameterRedactionMode redactionMode, string expectedHttpPath)
    {
        using ManualResetEventSlim serverWaitForClientEvent = new();
        using ManualResetEventSlim clientWaitForServerEvent = new();

        using var testServer = TestHttpServer.RunServerOrThrow(
            ctx =>
            {
                clientWaitForServerEvent.Set();
                serverWaitForClientEvent.Wait();
                try
                {
                    ctx.Response.OutputStream.Close();
                }
                catch (HttpListenerException)
                {
                    // HttpListenerException exception type might be thrown if the underlying network connection was already closed.
                }
            },
            out var hostName,
            out var port);

        using var traceProcessor = new TestTraceProcessor();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient()
                .AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .AddHttpClientTracing(options => options.RequestPathParameterRedactionMode = redactionMode)
                        .AddTestTraceProcessor(traceProcessor);
                }))
            .StartAsync();

        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();
        string requestUrl = $"{_httpPrefix}{hostName}:{port}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{requestUrl}/api/sensitive_id");
        request.SetRequestMetadata(new RequestMetadata { RequestRoute = "api/{id}" });

        try
        {
            using var tokenSource = new CancellationTokenSource();
            var task = httpClient.SendAsync(request, tokenSource.Token);
            clientWaitForServerEvent.Wait();
            tokenSource.Cancel();
            serverWaitForClientEvent.Set();
            await task;
        }
        catch (OperationCanceledException)
        {
            // no op.
        }

        var activity = traceProcessor.FirstActivity;
        Assert.NotNull(activity);
        Assert.Equal($"{requestUrl}/{expectedHttpPath}", activity.GetTagItem(Constants.AttributeHttpUrl));
        activity.AssertSensitiveTagsAreNull();
    }
}
#endif

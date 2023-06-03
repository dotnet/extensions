// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
#if NETFRAMEWORK
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
#endif
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.AspNetCore.Telemetry.Test.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if !NETFRAMEWORK
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
#endif
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using OpenTelemetry.Trace;
using Xunit;
using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class HttpUrlRedactionProcessorTests
{
    private const string TestHost = "localhost";
    private const string TestUrl = $"http://{TestHost}";

    private readonly ILogger<HttpUrlRedactionProcessor> _logger = new Mock<ILogger<HttpUrlRedactionProcessor>>().Object;

    [Fact]
    public void HttpUrlRedactionProcessor_NullOptionsThrows()
    {
        var routeParser = GetHttpRouteParser();
        var routeFormatter = GetHttpRouteFormatter();
        var routeUtility = GetHttpRouteUtility();

        Assert.Throws<ArgumentException>(() =>
            new HttpUrlRedactionProcessor(MSOptions.Options.Create<HttpTracingOptions>(null!), routeFormatter, routeParser, routeUtility, _logger));
    }

    [Fact]
    public void HttpUrlRedactionProcessor_ProcessResponse_WithNullRouteData_SetsUrlWithEmptyPath()
    {
        var httpTracingOptions = new HttpTracingOptions();
        var options = MSOptions.Options.Create(httpTracingOptions);
        var routeParser = GetHttpRouteParser();
        var routeFormatter = GetHttpRouteFormatter();
        var routeUtility = GetHttpRouteUtility();
        var logger = new FakeLogger<HttpUrlRedactionProcessor>();
        var redactionProcessor = new HttpUrlRedactionProcessor(options, routeFormatter, routeParser, routeUtility, logger);

        using Activity activity = new Activity("test");
        var request = GetMockedHttpRequest("/users/testUserId/chats/testChatId");

#if NETCOREAPP3_1_OR_GREATER
        redactionProcessor.ProcessResponse(activity, request);
#else
        activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, request);
        using var enrichmentProcessor = new HttpTraceEnrichmentProcessor(redactionProcessor, Enumerable.Empty<IHttpTraceEnricher>());
        enrichmentProcessor.OnEnd(activity);
#endif

        Assert.Equal($"http://localhost/{Constants.RequestNameUnknown}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal(Constants.RequestNameUnknown, activity.DisplayName);

        ValidateLoggerRecord(logger, "test", 2);
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict,
        $"/api/users/xxxuser123xxx/unread/chats/{TelemetryConstants.Redacted}/messages",
        $"/api/users/{{userId}}/unread/chats/{{chatId}}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.Loose,
        $"/api/users/xxxuser123xxx/unread/chats/chat123/messages",
        $"/api/users/{{userId}}/unread/chats/{{chatId}}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.None,
        $"/api/users/user123/unread/chats/chat123/messages",
        $"/api/users/user123/unread/chats/chat123/messages")]
    public async Task RedactionModeIsRespected(HttpRouteParameterRedactionMode mode, string expectedPath, string expectedName)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder => builder
                .AddHttpTracing(
                    options =>
                    {
                        options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                        options.IncludePath = true;
                        options.RequestPathParameterRedactionMode = mode;
                    })
                .AddTestTraceProcessor(testTraceProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/api/users/user123/unread/chats/chat123/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}{expectedPath}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal(expectedName, activity.DisplayName);
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_ExportsRouteAndParameters()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing()
                .AddTestTraceProcessor(testTraceProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/some/route/{{routeId}}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/some/route/{routeId}", activity.DisplayName);

        Assert.Equal(TelemetryConstants.Redacted, activity.GetTagItem("routeId"));
        Assert.Equal(5, activity.TagObjects.Count());
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_WhenRedactedLengthIsTooLong_ReturnsConstant()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder => builder
                .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                .AddTestTraceProcessor(testTraceProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "Redacted: {0}", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/api/users/{{userId}}/unread/chats/{{chatId}}/messages", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", activity.DisplayName);
        Assert.StartsWith("Redacted:", (string)activity.GetTagItem("userId")!);
        Assert.Equal(TelemetryConstants.Redacted, activity.GetTagItem("chatId"));
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_WithTagsToRedactAndHttpRoute_MatchingParametersGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/users/{userId}/chats/{chatId}",
            builder => builder
                .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>(),
            _ => { });
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/users/testUserId/chats/testChatId").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/users/{{userId}}/chats/{{chatId}}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/users/{userId}/chats/{chatId}", activity.DisplayName);
        Assert.StartsWith(string.Empty, (string)activity.GetTagItem("userId")!);
        Assert.Equal(TelemetryConstants.Redacted, activity.GetTagItem("chatId"));
    }

    [Fact]
    public async Task RedactParameter_WithNonPreciseRedactor_MatchingParameterGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/get_user/{userId}",
            builder => builder
                .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                .AddTestTraceProcessor(testTraceProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "Redacted: {0}", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/get_user/testUserId").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/get_user/{{userId}}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/get_user/{userId}", activity.DisplayName);
        Assert.StartsWith("Redacted: ", activity.GetTagItem("userId")?.ToString());
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"api/users/xxxtestUserIdxxx/unread/chats/{TelemetryConstants.Redacted}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "api/users/xxxtestUserIdxxx/unread/chats/testChatId/messages")]
    public async Task IncludeFormattedUrlEnabled_WithTagsToRedact_MatchingParametersGetsRedacted(HttpRouteParameterRedactionMode mode, string expectedPath)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder => builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RequestPathParameterRedactionMode = mode;
                    options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                })
                .AddTestTraceProcessor(testTraceProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));
        Assert.Null(activity.GetTagItem("chatId"));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/{expectedPath}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", activity.DisplayName);
        Assert.NotEqual(TelemetryConstants.Redacted, activity.GetTagItem("userId"));
        Assert.NotEqual("testUserId", activity.GetTagItem("userId"));
    }

    [Fact]
    public async Task IncludeFormattedUrlEnabled_WithTagsToRedactAndHttpRoute_MatchingParametersGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder => builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                })
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>(),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);

        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));
        Assert.Null(activity.GetTagItem("chatId"));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/api/users/xxxtestUserIdxxx/unread/chats/REDACTED/messages", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", activity.DisplayName);
        Assert.NotEqual("testUserId", activity.GetTagItem("userId"));
        Assert.NotEqual(TelemetryConstants.Redacted, activity.GetTagItem("userId"));
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"some/route/{TelemetryConstants.Redacted}")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, $"some/route/123")]
    public async Task AddHttpTracing_WithIncludeFormattedUrlEnabled_ExportsFormattedUrl(HttpRouteParameterRedactionMode mode, string expectedPath)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RequestPathParameterRedactionMode = mode;
                })
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>(),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);

        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/{expectedPath}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/some/route/{routeId}", activity.DisplayName);
    }

    [Fact]
    public async Task AddHttpTracing_WithConfigSection_ExportsFormattedUrl()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpTracingOptions");

        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing(configSection)
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher(new TestEnricher()),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));

        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var enrichmentProcessor = provider.Services!.GetRequiredService<HttpTraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = testTraceProcessor.FirstActivity;
        Assert.NotNull(activity);

        Assert.Null(activity.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));

        Assert.Equal(HttpMethod.Get.Method, activity.GetTagItem(Constants.AttributeHttpMethod));
        Assert.Equal(TestHost, activity.GetTagItem(Constants.AttributeHttpHost));
        Assert.Equal($"{TestUrl}/some/route/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Equal((int)HttpStatusCode.OK, activity.GetTagItem(Constants.AttributeHttpStatusCode));
        Assert.Equal("/some/route/{routeId}", activity.DisplayName);
    }

    [Fact]
    public async Task AddHttpTracing_WithIncludeFormattedUrlEnabled_ExcludedRouteIsNotExported()
    {
        using var testExporter = new TestExporter();
        using var exportProcessor = new WrappedActivityExportProcessor(testExporter);

        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing(o =>
                {
                    o.IncludePath = true;
                    o.ExcludePathStartsWith.Add("/some/route/{routeId}");
                })
                .AddHttpTraceEnricher<TestEnricher>()
                .AddTestTraceProcessor(exportProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForExportProcessorInvocations(exportProcessor);

        Assert.True(exportProcessor.IsInvoked);
        Assert.False(testExporter.IsInvoked);
        Assert.Equal(0, (int)(exportProcessor.FirstActivity!.ActivityTraceFlags & ActivityTraceFlags.Recorded));
    }

    [Fact]
    public async Task AddHttpTracing_WithIncludeFormattedUrlDisabled_ExcludedRouteIsNotExported()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpTracingOptionsWithExcludedRoute");

        using var testExporter = new TestExporter();
        using var exportProcessor = new WrappedActivityExportProcessor(testExporter);

        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing(configSection)
                .AddHttpTraceEnricher<TestEnricher>()
                .AddTestTraceProcessor(exportProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));

        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForExportProcessorInvocations(exportProcessor);

        Assert.True(exportProcessor.IsInvoked);
        Assert.False(testExporter.IsInvoked);
        Assert.Equal(0, (int)(exportProcessor.FirstActivity!.ActivityTraceFlags & ActivityTraceFlags.Recorded));
    }

    [Fact]
    public async Task AddHttpTracing_WithExcludePathStartsWith_NotMatchedRouteExported()
    {
        using var testExporter = new TestExporter();
        using var exportProcessor = new WrappedActivityExportProcessor(testExporter);

        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder => builder
                .AddHttpTracing(o =>
                {
                    o.IncludePath = true;
                    o.ExcludePathStartsWith.Add("/route/{routeId}");
                })
                .AddTestTraceProcessor(exportProcessor),
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));

        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForExportProcessorInvocations(exportProcessor);

        Assert.True(exportProcessor.IsInvoked);
        Assert.True(testExporter.IsInvoked);
        Assert.Equal(ActivityTraceFlags.Recorded, exportProcessor.FirstActivity!.ActivityTraceFlags & ActivityTraceFlags.Recorded);
    }
#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public async Task AddHttpTraceEnricher_WithException_CallsEnrichMethodOnce()
    {
        var exportedItems = new List<Activity>();

        var mockEnricher = new Mock<IHttpTraceEnricher>();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddOpenTelemetry().WithTracing(builder => builder
                        .AddHttpTraceEnricher(mockEnricher.Object)
                        .AddHttpTracing()
                        .AddInMemoryExporter(exportedItems)))
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGet("/error", context => throw new InvalidOperationException("CustomException")))))
            .StartAsync();

        using var client = host.GetTestClient();

        try
        {
            using var response = await client.GetAsync(new Uri("/error", UriKind.Relative)).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 1);

        var activity = exportedItems.Single();
        mockEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpRequest>()), Times.Once);
    }

    [Fact]
    public async Task AddHttpTracing_WithException_AddsExceptionInformation()
    {
        var exportedItems = new List<Activity>();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddOpenTelemetry().WithTracing(builder => builder
                        .AddHttpTracing()
                        .AddInMemoryExporter(exportedItems)))
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGet("/error", context => throw new InvalidOperationException("CustomException")))))
            .StartAsync();

        using var client = host.GetTestClient();

        try
        {
            using var response = await client.GetAsync(new Uri("/error", UriKind.Relative)).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 1);

        var activity = exportedItems.Single();
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem(Constants.AttributeExceptionType));
        Assert.Contains("CustomException", (string?)activity.GetTagItem(Constants.AttributeExceptionMessage));
    }

    [Fact]
    public async Task AddHttpTraceEnricher_WhenValidRequestNotSent_DoesNotExportActivity()
    {
        var exportedItems = new List<Activity>();
        var mockEnricher = new Mock<IHttpTraceEnricher>();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddOpenTelemetry().WithTracing(builder => builder
                        .AddHttpTraceEnricher(mockEnricher.Object)
                        .AddHttpTracing()
                        .AddInMemoryExporter(exportedItems)))
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGet("/api", context => context.Response.WriteAsync("GetCompleted")))))
            .StartAsync();

        using var client = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api", UriKind.Relative));
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        WaitForActivityExport(exportedItems, 1);
        try
        {
            using var response2 = await client.SendAsync(request).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 2);
        Assert.Single(exportedItems);

        mockEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpRequest>()), Times.Once);
    }

#else
    [Fact]
    public async Task AddHttpTraceEnricher_WithException_CallsEnrichMethodOnce()
    {
        var mockEnricher = new Mock<IHttpTraceEnricher>();
        var exportedItems = new List<Activity>();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpTraceEnricher(mockEnricher.Object)
                    .AddHttpTracing()
                    .AddInMemoryExporter(exportedItems)))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes =>
                {
                    routes.MapGet("/error", context => throw new InvalidOperationException("CustomException"));
                })
                .UseMvc());

        using var server = new TestServer(webHostBuilder);
        await server.Host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();

        try
        {
            using var response = await client.GetAsync(new Uri("/error", UriKind.Relative)).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 1);

        var activity = exportedItems.Single();
        mockEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpRequest>()), Times.Once);

        await server.Host.StopAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AddHttpTracing_WithException_AddsExceptionInformation()
    {
        var exportedItems = new List<Activity>();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpTracing()
                    .AddInMemoryExporter(exportedItems)))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes =>
                {
                    routes.MapGet("/error", context => throw new InvalidOperationException("CustomException"));
                })
                .UseMvc());

        using var server = new TestServer(webHostBuilder);
        await server.Host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();

        try
        {
            using var response = await client.GetAsync(new Uri("/error", UriKind.Relative)).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 1);

        var activity = exportedItems.Single();
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem(Constants.AttributeExceptionType));
        Assert.Contains("CustomException", (string?)activity.GetTagItem(Constants.AttributeExceptionMessage));

        await server.Host.StopAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AddHttpTraceEnricher_WhenValidRequestNotSent_DoesNotExportActivity()
    {
        var exportedItems = new List<Activity>();
        var mockEnricher = new Mock<IHttpTraceEnricher>();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddFakeRedaction(options => options.RedactionFormat = "RedactedData:{0}")
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpTraceEnricher(mockEnricher.Object)
                    .AddHttpTracing()
                    .AddInMemoryExporter(exportedItems)))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes =>
                {
                    routes.MapGet("/api", context => context.Response.WriteAsync("GetCompleted"));
                })
                .UseMvc());

        using var server = new TestServer(webHostBuilder);
        await server.Host.StartAsync().ConfigureAwait(false);
        using var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api", UriKind.Relative));
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        try
        {
            using var response2 = await client.SendAsync(request).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // an exception was thrown and caught here for the purpose of this test
        }

        WaitForActivityExport(exportedItems, 2);
        Assert.Single(exportedItems);
        mockEnricher.Verify(e => e.Enrich(It.IsAny<Activity>(), It.IsAny<HttpRequest>()), Times.Once);

        await server.Host.StopAsync().ConfigureAwait(false);
    }

#endif

    private static void WaitForProcessorInvocations(TestTraceProcessor testTraceProcessor)
    {
        // We need to let End callback execute as it is executed AFTER response was returned.
        // In unit tests environment there may be a lot of parallel unit tests executed, so
        // giving some breezing room for the End callback to complete
        SpinWait.SpinUntil(
            () =>
            {
                Thread.Sleep(10);
                return testTraceProcessor.IsProcessorInvoked;
            },
            TimeSpan.FromSeconds(10));
    }

    private static void WaitForExportProcessorInvocations(WrappedActivityExportProcessor testTraceProcessor)
    {
        // We need to let End callback execute as it is executed AFTER response was returned.
        // In unit tests environment there may be a lot of parallel unit tests executed, so
        // giving some breezing room for the End callback to complete
        SpinWait.SpinUntil(
            () =>
            {
                Thread.Sleep(10);
                return testTraceProcessor.IsInvoked;
            },
            TimeSpan.FromSeconds(10));
    }

    private static void WaitForActivityExport(List<Activity> exportedItems, int count)
    {
        // We need to let End callback execute as it is executed AFTER response was returned.
        // In unit tests environment there may be a lot of parallel unit tests executed, so
        // giving some breezing room for the End callback to complete
        SpinWait.SpinUntil(
            () =>
            {
                Thread.Sleep(10);
                return exportedItems.Count >= count;
            },
            TimeSpan.FromSeconds(10));
    }

    private static void ValidateLoggerRecord(FakeLogger logger, string activityName, int eventId)
    {
        FakeLogCollector collector = logger.Collector;
        Assert.Equal(2, collector.Count);

        FakeLogRecord record = collector.LatestRecord;
        Assert.Equal(eventId, record.Id.Id);
        Assert.Equal(LogLevel.Debug, record.Level);
        Assert.Contains(activityName, record.Message);
    }

    private static IHttpRouteParser GetHttpRouteParser()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .BuildServiceProvider();

        return builder.GetService<IHttpRouteParser>()!;
    }

    private static IHttpRouteFormatter GetHttpRouteFormatter()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .BuildServiceProvider();

        return builder.GetService<IHttpRouteFormatter>()!;
    }

    private static IIncomingHttpRouteUtility GetHttpRouteUtility()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteUtilities()
            .BuildServiceProvider();

        return builder.GetService<IIncomingHttpRouteUtility>()!;
    }

    private static HttpRequest GetMockedHttpRequest(string path = "/")
    {
        var httpContextMock = new Mock<HttpContext>(MockBehavior.Default);
        httpContextMock.Setup(h => h.Features.Get<IEndpointFeature>()).Returns((IEndpointFeature)null!);

        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.HttpContext).Returns(httpContextMock.Object);
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        requestMock.SetupGet(r => r.Scheme).Returns("http");
        requestMock.SetupGet(r => r.Path).Returns(new PathString(path));

        return requestMock.Object;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.AspNetCore.Telemetry.Test.Internal;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;
using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class HttpUrlRedactionProcessorTests
{
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
    public void HttpUrlRedactionProcessor_OnEnd_WithNullEndpoint_SetsTargetToNull()
    {
        var httpTracingOptions = new HttpTracingOptions
        {
            IncludePath = true,
        };
        var options = MSOptions.Options.Create(httpTracingOptions);
        var routeParser = GetHttpRouteParser();
        var routeFormatter = GetHttpRouteFormatter();
        var routeUtility = GetHttpRouteUtility();
        var processor = new HttpUrlRedactionProcessor(options, routeFormatter, routeParser, routeUtility, _logger);

        using Activity activity = new Activity("test");
        activity.AddTag(Constants.AttributeHttpPath, "/users/testUserId/chats/testChatId");
        activity.AddTag(Constants.AttributeHttpUrl, "http://localhost/users/testUserId/chats/testChatId");
        activity.AddTag(Constants.AttributeHttpTarget, "/users/testUserId/chats/testChatId");
        activity.AddTag("http.status_code", 200);

        processor.Process(activity, GetMockedHttpRequest());

        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Fact]
    public void HttpUrlRedactionProcessor_OnEnd_WithNullRouteData_SetsTargetToNull()
    {
        var httpTracingOptions = new HttpTracingOptions();
        var options = MSOptions.Options.Create(httpTracingOptions);
        var routeParser = GetHttpRouteParser();
        var routeFormatter = GetHttpRouteFormatter();
        var routeUtility = GetHttpRouteUtility();
        var logger = new FakeLogger<HttpUrlRedactionProcessor>();
        var processor = new HttpUrlRedactionProcessor(options, routeFormatter, routeParser, routeUtility, logger);

        using Activity activity = new Activity("test");
        activity.AddTag(Constants.AttributeHttpPath, "/users/testUserId/chats/testChatId");
        activity.AddTag(Constants.AttributeHttpUrl, "http://localhost/users/testUserId/chats/testChatId");
        activity.AddTag(Constants.AttributeHttpTarget, "/users/testUserId/chats/testChatId");
        activity.AddTag("http.status_code", 200);
        activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, GetMockedHttpRequest());

        processor.Process(activity, GetMockedHttpRequest());

        Assert.True(string.IsNullOrEmpty((string?)activity.GetTagItem(Constants.AttributeHttpRoute)));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpUrl));

        ValidateLoggerRecord(logger, "test", 2);
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict,
        $"api/users/xxxuser123xxx/unread/chats/{TelemetryConstants.Redacted}/messages",
        $"/api/users/{{userId}}/unread/chats/{{chatId}}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.Loose,
        $"api/users/xxxuser123xxx/unread/chats/chat123/messages",
        $"/api/users/{{userId}}/unread/chats/{{chatId}}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.None,
        $"/api/users/user123/unread/chats/chat123/messages",
        $"/api/users/user123/unread/chats/chat123/messages")]
    public async Task RedactionModeIsRespected(HttpRouteParameterRedactionMode mode, string expectedPath, string expectedName)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder =>
            {
                builder
                .AddHttpTracing(
                    options =>
                    {
                        options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                        options.IncludePath = true;
                        options.RequestPathParameterRedactionMode = mode;
                    })
                .AddTestTraceProcessor(testTraceProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/api/users/user123/unread/chats/chat123/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Equal(expectedPath, testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal(expectedName, testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_ExportsRouteAndParameters()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder =>
            {
                builder
                .AddHttpTracing()
                .AddTestTraceProcessor(testTraceProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.Equal(TelemetryConstants.Redacted, testTraceProcessor.FirstActivity!.GetTagItem("routeId"));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.DisplayName);
        Assert.Equal(6, testTraceProcessor.FirstActivity!.TagObjects.Count());
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_WhenRedactedLengthIsTooLong_ReturnsContstant()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder =>
            {
                builder
                .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                .AddTestTraceProcessor(testTraceProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "Redacted: {0}", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.StartsWith("Redacted:", (string)testTraceProcessor.FirstActivity!.GetTagItem("userId")!);
        Assert.Equal(TelemetryConstants.Redacted, testTraceProcessor.FirstActivity!.GetTagItem("chatId"));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task IncludeFormattedUrlDisabled_WithTagsToRedactAndHttpRoute_MatchingParametersGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/users/{userId}/chats/{chatId}",
            builder =>
            {
                builder
                .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>();
            },
            _ => { });
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/users/testUserId/chats/testChatId").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/users/{userId}/chats/{chatId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal("/users/{userId}/chats/{chatId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.Equal(string.Empty, testTraceProcessor.FirstActivity!.GetTagItem("userId"));
        Assert.Equal(TelemetryConstants.Redacted, testTraceProcessor.FirstActivity!.GetTagItem("chatId"));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/users/{userId}/chats/{chatId}", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task RedactParameter_WithNonPreciseRedactor_MatchingParameterGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/get_user/{userId}",
            builder =>
            {
                builder
                    .AddHttpTracing(options => options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData))
                    .AddTestTraceProcessor(testTraceProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "Redacted: {0}", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/get_user/testUserId").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/get_user/{userId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal("/get_user/{userId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.StartsWith("Redacted: ", testTraceProcessor.FirstActivity!.GetTagItem("userId")?.ToString());
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/get_user/{userId}", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"api/users/xxxtestUserIdxxx/unread/chats/{TelemetryConstants.Redacted}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "api/users/xxxtestUserIdxxx/unread/chats/testChatId/messages")]
    public async Task IncludeFormattedUrlEnabled_WithTagsToRedact_MatchingParametersGetsRedacted(HttpRouteParameterRedactionMode mode, string expectedPath)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder =>
            {
                builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RequestPathParameterRedactionMode = mode;
                    options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                })
                .AddTestTraceProcessor(testTraceProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal(expectedPath, testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.NotEqual("testUserId", testTraceProcessor.FirstActivity!.GetTagItem("userId"));
        Assert.NotEqual(TelemetryConstants.Redacted, testTraceProcessor.FirstActivity!.GetTagItem("userId"));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem("chatId"));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task IncludeFormattedUrlEnabled_WithTagsToRedactAndHttpRoute_MatchingParametersGetsRedacted()
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/api/users/{userId}/unread/chats/{chatId}/messages",
            builder =>
            {
                builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
                })
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>();
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", SimpleClassifications.PrivateData));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/api/users/testUserId/unread/chats/testChatId/messages").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal($"api/users/xxxtestUserIdxxx/unread/chats/{TelemetryConstants.Redacted}/messages", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.NotEqual("testUserId", testTraceProcessor.FirstActivity!.GetTagItem("userId"));
        Assert.NotEqual(TelemetryConstants.Redacted, testTraceProcessor.FirstActivity!.GetTagItem("userId"));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem("chatId"));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/api/users/{userId}/unread/chats/{chatId}/messages", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"some/route/{TelemetryConstants.Redacted}")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, $"some/route/123")]
    public async Task AddHttpTracing_WithIncludeFormattedUrlEnabled_ExportsFormattedUrl(HttpRouteParameterRedactionMode mode, string expectedPath)
    {
        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder =>
            {
                builder
                .AddHttpTracing(options =>
                {
                    options.IncludePath = true;
                    options.RequestPathParameterRedactionMode = mode;
                })
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher<TestEnricher>();
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));
        using var client = await provider.GetHttpClientAsync();

        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal(expectedPath, testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task AddHttpTracing_WithConfigSection_ExportsFormattedUrl()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpTracingOptions");

        using var testTraceProcessor = new TestTraceProcessor();
        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder =>
            {
                builder
                .AddHttpTracing(configSection)
                .AddTestTraceProcessor(testTraceProcessor)
                .AddHttpTraceEnricher(new TestEnricher());
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));

        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForProcessorInvocations(testTraceProcessor);

        var enrichmentProcessor = provider.Services!.GetRequiredService<HttpTraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpUrl));
        Assert.Null(testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpRoute));
        Assert.Equal($"some/route/{TelemetryConstants.Redacted}", testTraceProcessor.FirstActivity!.GetTagItem(Constants.AttributeHttpPath));
        Assert.Equal(200, testTraceProcessor.FirstActivity!.GetTagItem("http.status_code"));
        Assert.Equal("/some/route/{routeId}", testTraceProcessor.FirstActivity!.DisplayName);
    }

    [Fact]
    public async Task AddHttpTracing_WithIncludeFormattedUrlEnabled_ExcludedRouteIsNotExported()
    {
        using var testExporter = new TestExporter();
        using var exportProcessor = new WrappedActivityExportProcessor(testExporter);

        using var provider = new TestHttpClientProvider(
            "/some/route/{routeId}",
            builder =>
            {
                builder
                .AddHttpTracing(o =>
                {
                    o.IncludePath = true;
                    o.ExcludePathStartsWith.Add("/some/route/{routeId}");
                })
                .AddHttpTraceEnricher<TestEnricher>()
                .AddTestTraceProcessor(exportProcessor);
            },
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
            builder =>
            {
                builder
                .AddHttpTracing(configSection)
                .AddHttpTraceEnricher<TestEnricher>()
                .AddTestTraceProcessor(exportProcessor);
            },
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
            builder =>
            {
                builder
                    .AddHttpTracing(o =>
                    {
                        o.IncludePath = true;
                        o.ExcludePathStartsWith.Add("/route/{routeId}");
                    })
                    .AddTestTraceProcessor(exportProcessor);
            },
            redaction => redaction.SetFakeRedactor(x => x.RedactionFormat = "xxx{0}xxx", Array.Empty<DataClassification>()));

        using var client = await provider.GetHttpClientAsync();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        WaitForExportProcessorInvocations(exportProcessor);

        Assert.True(exportProcessor.IsInvoked);
        Assert.True(testExporter.IsInvoked);
        Assert.Equal(ActivityTraceFlags.Recorded, exportProcessor.FirstActivity!.ActivityTraceFlags & ActivityTraceFlags.Recorded);
    }

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

    private static HttpRequest GetMockedHttpRequest()
    {
        var httpContextMock = new Mock<HttpContext>(MockBehavior.Default);
        httpContextMock.Setup(h => h.Features.Get<IEndpointFeature>()).Returns((IEndpointFeature)null!);

        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.HttpContext).Returns(httpContextMock.Object);

        return requestMock.Object;
    }
}

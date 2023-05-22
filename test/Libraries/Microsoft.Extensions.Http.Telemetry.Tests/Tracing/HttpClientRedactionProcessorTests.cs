// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;
using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test;

public class HttpClientRedactionProcessorTests
{
    [Fact]
    public void HttpClientRedactionProcessor_NullOptions_Throws()
    {
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var redactor = new Mock<IHttpPathRedactor>().Object;
        Assert.Throws<ArgumentException>(() => new HttpClientRedactionProcessor(
            MSOptions.Options.Create<HttpClientTracingOptions>(null!), redactor, requestMetadataContext));
    }

    [Theory]
    [CombinatorialData]
    public void HttpClientRedactionProcessor_GivenNullRequestUri_DoesNotSetHttpTargetAndLogsError(bool isLoggerPresent)
    {
        const int EventId = 2;
        const string ActivityName = "test";

        var redactor = GetHttpPathRedactor();
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        using var listener = new TestEventListener(HttpTracingEventSource.Instance);
        var logger = isLoggerPresent ? new FakeLogger<HttpClientRedactionProcessor>() : null;

        var options = MSOptions.Options.Create(new HttpClientTracingOptions());
        var processor = new HttpClientRedactionProcessor(options, redactor, requestMetadataContext, logger: logger);

        using Activity activity = new Activity(ActivityName);

        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));

        ValidateEventSourceRecord(listener, EventId, EventLevel.Error, ActivityName);

        if (isLoggerPresent)
        {
            ValidateLoggerRecord(logger!, EventId, LogLevel.Error, ActivityName);
        }
    }

    [Fact]
    public void HttpClientRedactionProcessor_UrlContains_ParametersInTagsToRedactList_RedactsInExportedUrl()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var redactorProvider = (builder.GetService<IRedactorProvider>() as FakeRedactorProvider)!;
        var options = new HttpClientTracingOptions();
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}",
            RequestName = "TestRequestName"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal("TestRequestName", activity.DisplayName);
        Assert.Equal(SimpleClassifications.PrivateData, redactorProvider.Collector.LastRedactorRequested.DataClassification);
        Assert.Equal("routeId123", redactorProvider.Collector.LastRedactedData.Original);
        Assert.Equal($"http://test.com/api/routes/Redacted:routeId123/chats/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict,
        $"http://test.com/api/routes/Redacted:routeId123/chats/{TelemetryConstants.Redacted}",
        $"api/routes/Redacted:routeId123/chats/{TelemetryConstants.Redacted}")]
    [InlineData(HttpRouteParameterRedactionMode.Loose,
        $"http://test.com/api/routes/Redacted:routeId123/chats/chatId123",
        $"api/routes/Redacted:routeId123/chats/chatId123")]
    [InlineData(HttpRouteParameterRedactionMode.None,
        $"http://test.com/api/routes/routeId123/chats/chatId123",
        $"/api/routes/routeId123/chats/chatId123")]
    public void HttpClientRedactionProcessor_RequestNameMissing_SetsRedactedPathAsDisplayName(
        HttpRouteParameterRedactionMode httpPathParameterRedactionMode,
        string exptectedUrl,
        string exptectedActivityName)
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .Configure<HttpClientTracingOptions>(o => o.RequestPathParameterRedactionMode = httpPathParameterRedactionMode)
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var redactorProvider = (builder.GetService<IRedactorProvider>() as FakeRedactorProvider)!;
        var options = builder.GetRequiredService<IOptions<HttpClientTracingOptions>>().Value;
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(exptectedActivityName, activity.DisplayName);
        Assert.Equal(exptectedUrl, activity.GetTagItem(Constants.AttributeHttpUrl));

        if (httpPathParameterRedactionMode != HttpRouteParameterRedactionMode.None)
        {
            Assert.Equal(SimpleClassifications.PrivateData, redactorProvider.Collector.LastRedactorRequested.DataClassification);
            Assert.Equal("routeId123", redactorProvider.Collector.LastRedactedData.Original);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => redactorProvider.Collector.LastRedactorRequested.DataClassification);
        }
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict)]
    [InlineData(HttpRouteParameterRedactionMode.Loose)]
    public void HttpClientRedactionProcessor_UrlContains_ParametersInTagsToRedactList_RequestRouteMissing_ExportsConstant(
        HttpRouteParameterRedactionMode httpPathParameterRedactionMode)
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .Configure<HttpClientTracingOptions>(o => o.RequestPathParameterRedactionMode = httpPathParameterRedactionMode)
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var httpRouteParser = builder.GetService<IHttpRouteParser>();
        var httpRouteFormatter = builder.GetService<IHttpRouteFormatter>();
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(TelemetryConstants.Unknown, activity.DisplayName);
        Assert.Equal($"http://test.com/{TelemetryConstants.Unknown}", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict)]
    [InlineData(HttpRouteParameterRedactionMode.Loose)]
    public void HttpClientRedactionProcessor_UrlContains_AllParametersInTagsToRedactList_RedactsAllParamsInExportedUrl(
        HttpRouteParameterRedactionMode httpPathParameterRedactionMode)
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .Configure<HttpClientTracingOptions>(o => o.RequestPathParameterRedactionMode = httpPathParameterRedactionMode)
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123/";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var redactorProvider = (builder.GetService<IRedactorProvider>() as FakeRedactorProvider)!;
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        options.RouteParameterDataClasses.Add("chatId", SimpleClassifications.PrivateData);
        options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal("api/routes/Redacted:routeId123/chats/Redacted:chatId123", activity.DisplayName);
        Assert.Equal(2, redactorProvider.Collector.AllRedactedData.Count);
        Assert.Equal("http://test.com/api/routes/Redacted:routeId123/chats/Redacted:chatId123", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict,
        $"http://test.com/api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}/messages",
        $"api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}/messages")]
    [InlineData(HttpRouteParameterRedactionMode.Loose,
        "http://test.com/api/routes/routeId123/chats/chatId123/messages",
        "api/routes/routeId123/chats/chatId123/messages")]
    public void HttpClientRedactionProcessor_DoesNotRedactNonParameterStringsInUrl(
        HttpRouteParameterRedactionMode httpPathParameterRedactionMode,
        string exptectedUrl,
        string exptectedActivityName)
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .Configure<HttpClientTracingOptions>(o => o.RequestPathParameterRedactionMode = httpPathParameterRedactionMode)
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123/messages";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var redactorProvider = new FakeRedactorProvider();
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routes", SimpleClassifications.PrivateData);
        options.RouteParameterDataClasses.Add("chats", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}/messages"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(exptectedActivityName, activity.DisplayName);
        Assert.Equal(0, redactorProvider.Collector.AllRedactorRequests.Count);
        Assert.Equal(0, redactorProvider.Collector.AllRedactedData.Count);
        Assert.Equal(exptectedUrl, activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Fact]
    public void HttpClientRedactionProcessor_UrlDoesNotContain_ParametersInTagsToRedactList_RedactsInExportedUrl()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var redactorProvider = (builder.GetService<IRedactorProvider>() as FakeRedactorProvider)!;
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal($"api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}", activity.DisplayName);
        Assert.Equal(0, redactorProvider.Collector.AllRedactorRequests.Count);
        Assert.Equal(0, redactorProvider.Collector.AllRedactedData.Count);
        Assert.Equal($"http://test.com/api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Fact]
    public void HttpClientRedactionProcessor_NoRequestRouteSet_ReturnsConstant()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test1");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(TelemetryConstants.Unknown, activity.DisplayName);
        Assert.Equal($"http://test.com/{TelemetryConstants.Unknown}", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Fact]
    public void HttpClientRedactionProcessor_NoRequestRouteSet_RequestNameSet_UsesRequestName()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test1");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestName = "TestRequest"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal("TestRequest", activity.DisplayName);
        Assert.Equal($"http://test.com/TestRequest", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Fact]
    public void HttpClientRedactionProcessor_EmptyRouteSet_ReturnsConstant()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var options = new HttpClientTracingOptions();
        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test1");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = string.Empty
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(TelemetryConstants.Redacted, activity.DisplayName);
        Assert.Equal($"http://test.com/{TelemetryConstants.Redacted}", activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict,
        $"http://test.com/api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}",
        $"api/routes/{TelemetryConstants.Redacted}/chats/{TelemetryConstants.Redacted}")]
    [InlineData(HttpRouteParameterRedactionMode.Loose,
        "http://test.com/api/routes/routeId123/chats/chatId123",
        "api/routes/routeId123/chats/chatId123")]
    public void HttpClientRedactorProcessor_Given_Zero_Tags_To_Redact_Returns_Quickly(
        HttpRouteParameterRedactionMode httpPathParameterRedactionMode,
        string exptectedUrl,
        string exptectedActivityName)
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .Configure<HttpClientTracingOptions>(o => o.RequestPathParameterRedactionMode = httpPathParameterRedactionMode)
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        const string UriString = "http://test.com/api/routes/routeId123/chats/chatId123";
        var httPathRedactor = builder.GetRequiredService<IHttpPathRedactor>();
        var redactorProvider = new FakeRedactorProvider();
        var options = new HttpClientTracingOptions();
        var requestMetadataContext = new Mock<IOutgoingRequestContext>().Object;
        var processor = new HttpClientRedactionProcessor(
            MSOptions.Options.Create(options),
            httPathRedactor,
            requestMetadataContext);

        using Activity activity = new Activity("test");
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri(UriString);

        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/api/routes/{routeId}/chats/{chatId}"
        });

        processor.Process(activity, httpRequestMessage);

        Assert.Equal(exptectedActivityName, activity.DisplayName);
        Assert.Equal(0, redactorProvider.Collector.AllRedactorRequests.Count);
        Assert.Equal(0, redactorProvider.Collector.AllRedactedData.Count);
        Assert.Equal(exptectedUrl, activity.GetTagItem(Constants.AttributeHttpUrl));
    }

    private static void ValidateEventSourceRecord(
        TestEventListener listener, int eventId, EventLevel level, string activityName)
    {
        EventWrittenEventArgs? lastEvent = listener.LastEvent;

        Assert.NotNull(lastEvent);
        Assert.Equal(eventId, lastEvent!.EventId);
        Assert.Equal(level, lastEvent!.Level);
        Assert.Contains(activityName, lastEvent!.Payload!);
    }

    private static void ValidateLoggerRecord(
        FakeLogger logger, int eventId, LogLevel level, string activityName)
    {
        FakeLogCollector collector = logger.Collector;
        Assert.Equal(2, collector.Count);

        FakeLogRecord record = collector.LatestRecord;
        Assert.Equal(eventId, record.Id.Id);
        Assert.Equal(level, record.Level);
        Assert.Contains(activityName, record.Message);
    }

    private static IHttpPathRedactor GetHttpPathRedactor()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddActivatedSingleton<IHttpPathRedactor, HttpPathRedactor>()
            .BuildServiceProvider();

        return builder.GetRequiredService<IHttpPathRedactor>();
    }
}

#endif

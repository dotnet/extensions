// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging.Test.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class AcceptanceTests
{
    private const string LoggingCategory = "Microsoft.Extensions.Http.Logging.HttpClientLogger";
    private static readonly Uri _unreachableRequestUri = new("https://we.wont.hit.this.domain.anyway");

    [Fact]
    public async Task AddHttpClientLogEnricher_WhenNullEnricherRegistered_SkipsNullEnrichers()
    {
        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction()
            .AddExtendedHttpClientLogging()
            .AddHttpClientLogEnricher<EnricherWithCounter>()
            .AddSingleton<IHttpClientLogEnricher>(static _ => null!)
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        using var _ = await httpClient.GetAsync(_unreachableRequestUri).ConfigureAwait(false);
        var collector = sp.GetFakeLogCollector();
        var logRecord = Assert.Single(collector.GetSnapshot());

        // No error should be logged:
        Assert.Equal(LogLevel.Information, logRecord.Level);
        Assert.Equal(LoggingCategory, logRecord.Category);
        Assert.Equal($"{HttpMethod.Get} {_unreachableRequestUri.Host}/{TelemetryConstants.Redacted}", logRecord.Message);
        Assert.Null(logRecord.Exception);

        var enrichers = sp.GetServices<IHttpClientLogEnricher>().ToList();
        var nullEnricher = Assert.Single(enrichers, x => x is null);
        Assert.Null(nullEnricher);

        var enricher = Assert.Single(enrichers, x => x is not null);
        var testEnricher = Assert.IsType<EnricherWithCounter>(enricher);
        Assert.Equal(1, testEnricher.TimesCalled);
    }

    [Fact]
    public async Task HttpClientLogger_WhenEnricherThrows_EmitsErrorAndKeepsExecution()
    {
        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction()
            .AddExtendedHttpClientLogging()
            .AddSingleton<IHttpClientLogEnricher, TestEnricher>(static _ => new TestEnricher(throwOnEnrich: true))
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        using var _ = await httpClient.GetAsync(_unreachableRequestUri).ConfigureAwait(false);
        var collector = sp.GetFakeLogCollector();
        Assert.Collection(
            collector.GetSnapshot(),
            static firstLogRecord =>
            {
                Assert.Equal(LogLevel.Error, firstLogRecord.Level);
                Assert.Equal(LoggingCategory, firstLogRecord.Category);
                Assert.StartsWith($"An error occurred in enricher '{typeof(TestEnricher).FullName}'", firstLogRecord.Message);
                Assert.EndsWith($"{HttpMethod.Get} {_unreachableRequestUri.Host}/{TelemetryConstants.Redacted}", firstLogRecord.Message);
                Assert.IsType<NotSupportedException>(firstLogRecord.Exception);
            },
            static secondLogRecord =>
            {
                // No error should be logged:
                Assert.Equal(LogLevel.Information, secondLogRecord.Level);
                Assert.Equal(LoggingCategory, secondLogRecord.Category);
                Assert.Equal($"{HttpMethod.Get} {_unreachableRequestUri.Host}/{TelemetryConstants.Redacted}", secondLogRecord.Message);
                Assert.Null(secondLogRecord.Exception);
            });
    }

    [Fact]
    public async Task AddHttpClientLogging_ServiceCollectionAndEnrichers_EnrichesLogsWithAllEnrichers()
    {
        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction()
            .AddExtendedHttpClientLogging()
            .AddHttpClientLogEnricher<EnricherWithCounter>()
            .AddHttpClientLogEnricher<TestEnricher>()
            .AddHttpClient("testClient").Services
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("testClient");
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        _ = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
        var collector = sp.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(logRecord => logRecord.Category == LoggingCategory);

        Assert.Equal($"{httpRequestMessage.Method} {httpRequestMessage.RequestUri.Host}/{TelemetryConstants.Redacted}", logRecord.Message);
        var enricher1 = sp.GetServices<IHttpClientLogEnricher>().SingleOrDefault(enn => enn is EnricherWithCounter) as EnricherWithCounter;
        var enricher2 = sp.GetServices<IHttpClientLogEnricher>().SingleOrDefault(enn => enn is TestEnricher) as TestEnricher;

        enricher1.Should().NotBeNull();
        enricher2.Should().NotBeNull();
        enricher1!.TimesCalled.Should().Be(1);

        var state = logRecord.StructuredState;
        state.Should().NotBeNull();
        state!.Single(kvp => kvp.Key == enricher2!.KvpRequest.Key).Value.Should().Be(enricher2!.KvpRequest.Value!.ToString());
    }

    [Fact]
    public async Task AddHttpClientLogging_WithNamedHttpClients_WorksCorrectly()
    {
        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient("namedClient1")
             .AddExtendedHttpClientLogging(o =>
             {
                 o.ResponseHeadersDataClasses.Add("ResponseHeader", FakeTaxonomy.PrivateData);
                 o.RequestHeadersDataClasses.Add("RequestHeader", FakeTaxonomy.PrivateData);
                 o.RequestHeadersDataClasses.Add("RequestHeaderFirst", FakeTaxonomy.PrivateData);
                 o.RequestBodyContentTypes.Add("application/json");
                 o.ResponseBodyContentTypes.Add("application/json");
                 o.LogBody = true;
             }).Services
             .AddHttpClient("namedClient2")
             .AddExtendedHttpClientLogging(o =>
             {
                 o.ResponseHeadersDataClasses.Add("ResponseHeader", FakeTaxonomy.PrivateData);
                 o.RequestHeadersDataClasses.Add("RequestHeader", FakeTaxonomy.PrivateData);
                 o.RequestHeadersDataClasses.Add("RequestHeaderSecond", FakeTaxonomy.PrivateData);
                 o.RequestBodyContentTypes.Add("application/json");
                 o.ResponseBodyContentTypes.Add("application/json");
                 o.LogBody = true;
             }).Services
             .BlockRemoteCall()
             .BuildServiceProvider();

        using var namedClient1 = provider.GetRequiredService<IHttpClientFactory>().CreateClient("namedClient1");
        using var namedClient2 = provider.GetRequiredService<IHttpClientFactory>().CreateClient("namedClient2");

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        httpRequestMessage.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage.Headers.Add("ReQuEStHeAdErFirst", new List<string> { "Request Value 2", "Request Value 3" });
        var responseString = await SendRequest(namedClient1, httpRequestMessage);
        var collector = provider.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");

        using var httpRequestMessage2 = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        httpRequestMessage2.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage2.Headers.Add("ReQuEStHeAdErSecond", new List<string> { "Request Value 2", "Request Value 3" });
        collector.Clear();
        responseString = await SendRequest(namedClient2, httpRequestMessage2);
        logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);
        state = logRecord.StructuredState;
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");
    }

    private static async Task<string> SendRequest(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
        using var content = await httpClient
            .SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead)
            .ConfigureAwait(false);

        var responseStream = await content.Content.ReadAsStreamAsync();
        var buffer = new byte[32768];
        _ = await responseStream.ReadAsync(buffer, 0, 32768);
        return Encoding.UTF8.GetString(buffer);
    }

    [Fact]
    public async Task AddHttpClientLogging_WithTypedHttpClients_WorksCorrectly()
    {
        await using var provider = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction()
            .AddSingleton<ITestHttpClient1, TestHttpClient1>()
            .AddSingleton<ITestHttpClient2, TestHttpClient2>()
            .AddHttpClient<ITestHttpClient1, TestHttpClient1>()
            .AddExtendedHttpClientLogging(x =>
            {
                x.ResponseHeadersDataClasses.Add("ResponseHeader", FakeTaxonomy.PrivateData);
                x.RequestHeadersDataClasses.Add("RequestHeader", FakeTaxonomy.PrivateData);
                x.RequestHeadersDataClasses.Add("RequestHeader2", FakeTaxonomy.PrivateData);
                x.RequestBodyContentTypes.Add("application/json");
                x.ResponseBodyContentTypes.Add("application/json");
                x.BodySizeLimit = 10000;
                x.LogBody = true;
            }).Services
            .AddHttpClient<ITestHttpClient2, TestHttpClient2>()
            .AddExtendedHttpClientLogging(x =>
            {
                x.ResponseHeadersDataClasses.Add("ResponseHeader", FakeTaxonomy.PrivateData);
                x.RequestHeadersDataClasses.Add("RequestHeader", FakeTaxonomy.PrivateData);
                x.RequestHeadersDataClasses.Add("RequestHeader2", FakeTaxonomy.PrivateData);
                x.RequestBodyContentTypes.Add("application/json");
                x.ResponseBodyContentTypes.Add("application/json");
                x.BodySizeLimit = 20000;
                x.LogBody = true;
            }).Services
            .BlockRemoteCall()
            .BuildServiceProvider();

        var firstClient = provider.GetService<ITestHttpClient1>() as TestHttpClient1;
        var secondClient = provider.GetService<ITestHttpClient2>() as TestHttpClient2;

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        httpRequestMessage.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage.Headers.Add("ReQuEStHeAdEr2", new List<string> { "Request Value 2", "Request Value 3" });
        var content = await firstClient!.SendRequest(httpRequestMessage).ConfigureAwait(false);
        var collector = provider.GetFakeLogCollector();
        var responseStream = await content.Content.ReadAsStreamAsync();
        var buffer = new byte[10000];
        _ = await responseStream.ReadAsync(buffer, 0, 10000);
        var responseString = Encoding.UTF8.GetString(buffer);

        var logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        state.Should().NotBeNull();
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");

        using var httpRequestMessage2 = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        httpRequestMessage2.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage2.Headers.Add("ReQuEStHeAdEr2", new List<string> { "Request Value 2", "Request Value 3" });
        collector.Clear();
        content = await secondClient!.SendRequest(httpRequestMessage2).ConfigureAwait(false);
        responseStream = await content.Content.ReadAsStreamAsync();
        buffer = new byte[20000];
        _ = await responseStream.ReadAsync(buffer, 0, 20000);
        responseString = Encoding.UTF8.GetString(buffer);

        logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);
        state = logRecord.StructuredState;
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, "v1/unit/REDACTED/users/REDACTED:123")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "v1/unit/999/users/REDACTED:123")]
    [InlineData(HttpRouteParameterRedactionMode.None, "/v1/unit/999/users/123")]
    public async Task AddHttpClientLogging_RedactSensitiveParams(HttpRouteParameterRedactionMode parameterRedactionMode, string redactedPath)
    {
        const string RequestPath = "https://fake.com/v1/unit/999/users/123";

        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction(o => o.RedactionFormat = "REDACTED:{0}")
            .AddHttpClient()
            .AddExtendedHttpClientLogging(o =>
            {
                o.RouteParameterDataClasses.Add("userId", FakeTaxonomy.PrivateData);
                o.RequestPathParameterRedactionMode = parameterRedactionMode;
            })
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(RequestPath),
        };

        var requestContext = sp.GetRequiredService<IOutgoingRequestContext>();
        requestContext.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/v1/unit/{unitId}/users/{userId}"
        });

        _ = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

        var collector = sp.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(logRecord => logRecord.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        state.Should().NotBeNull();
        state!.Single(kvp => kvp.Key == HttpClientLoggingTagNames.Path).Value.Should().Be(redactedPath);
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, "REDACTED", "<REDACTED:123>")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "999", "<REDACTED:123>")]
    [InlineData(HttpRouteParameterRedactionMode.None, "999", "123")]
    public async Task AddHttpClientLogging_StructuredPathLogging_RedactsSensitiveParams(
        HttpRouteParameterRedactionMode parameterRedactionMode,
        string expectedUnitId,
        string expectedUserId)
    {
        const string RequestPath = "https://fake.com/v1/unit/999/users/123";
        const string RequestRoute = "/v1/unit/{unitId}/users/{userId}";

        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction(o => o.RedactionFormat = "<REDACTED:{0}>")
            .AddHttpClient()
            .AddExtendedHttpClientLogging(o =>
            {
                o.RouteParameterDataClasses.Add("userId", FakeTaxonomy.PrivateData);
                o.RequestPathParameterRedactionMode = parameterRedactionMode;
                o.RequestPathLoggingMode = OutgoingPathLoggingMode.Structured;
            })
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(RequestPath)
        };

        httpRequestMessage.SetRequestMetadata(new RequestMetadata(httpRequestMessage.Method.ToString(), RequestRoute));

        using var _ = await httpClient.SendAsync(httpRequestMessage);

        var collector = sp.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(logRecord => logRecord.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        var loggedPath = state.Should().NotBeNull().And
            .ContainSingle(kvp => kvp.Key == HttpClientLoggingTagNames.Path)
            .Subject.Value;

        state.Should().ContainSingle(kvp => kvp.Key == HttpClientLoggingTagNames.Host)
            .Which.Value.Should().Be(httpRequestMessage.RequestUri.Host);

        state.Should().ContainSingle(kvp => kvp.Key == HttpClientLoggingTagNames.Method)
            .Which.Value.Should().Be(httpRequestMessage.Method.ToString());

        state.Should().ContainSingle(kvp => kvp.Key == HttpClientLoggingTagNames.StatusCode)
            .Which.Value.Should().Be("200");

        state.Should().ContainSingle(kvp => kvp.Key == HttpClientLoggingTagNames.Duration)
            .Which.Value.Should().NotBeEmpty();

        // When the redaction mode is set to "None", the RequestPathLoggingMode is ignored
        if (parameterRedactionMode == HttpRouteParameterRedactionMode.None)
        {
            loggedPath.Should().Be(httpRequestMessage.RequestUri.AbsolutePath);
            state.Should().HaveCount(5);
        }
        else
        {
            loggedPath.Should().Be(RequestRoute);
            state.Should().ContainSingle(kvp => kvp.Key == "userId").Which.Value.Should().Be(expectedUserId);
            state.Should().ContainSingle(kvp => kvp.Key == "unitId").Which.Value.Should().Be(expectedUnitId);
            state.Should().HaveCount(7);
        }
    }

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, "v1/unit/REDACTED/users/REDACTED:123")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "v1/unit/999/users/REDACTED:123")]
    public async Task AddHttpClientLogging_NamedHttpClient_RedactSensitiveParams(HttpRouteParameterRedactionMode parameterRedactionMode, string redactedPath)
    {
        const string RequestPath = "https://fake.com/v1/unit/999/users/123";

        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction(o => o.RedactionFormat = "REDACTED:{0}")
            .AddHttpClient("test")
            .AddExtendedHttpClientLogging(o =>
            {
                o.RouteParameterDataClasses.Add("userId", FakeTaxonomy.PrivateData);
                o.RequestPathParameterRedactionMode = parameterRedactionMode;
            })
            .Services
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(RequestPath),
        };

        var requestContext = sp.GetRequiredService<IOutgoingRequestContext>();
        requestContext.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/v1/unit/{unitId}/users/{userId}"
        });

        using var _ = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

        var collector = sp.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(logRecord => logRecord.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        state.Should().NotBeNull();
        state!.Single(kvp => kvp.Key == HttpClientLoggingTagNames.Path).Value.Should().Be(redactedPath);
    }

    [Fact]
    public void AddHttpClientLogging_WithNamedClients_RegistersNamedOptions()
    {
        const string FirstClientName = "1";
        const string SecondClientName = "2";

        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpClient(FirstClientName)
            .AddExtendedHttpClientLogging(options =>
            {
                options.LogRequestStart = true;
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test1", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient(SecondClientName)
            .AddExtendedHttpClientLogging(options =>
            {
                options.LogRequestStart = false;
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test2", FakeTaxonomy.PrivateData } };
            })
            .Services
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var firstClient = factory.CreateClient(FirstClientName);
        var secondClient = factory.CreateClient(SecondClientName);
        firstClient.Should().NotBe(secondClient);

        var optionsFirst = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(FirstClientName);
        var optionsSecond = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(SecondClientName);
        optionsFirst.Should().NotBeNull();
        optionsSecond.Should().NotBeNull();
        optionsFirst.Should().NotBeEquivalentTo(optionsSecond);
    }

    [Fact]
    public void AddHttpClientLogging_WithTypedClients_RegistersNamedOptions()
    {
        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddSingleton<ITestHttpClient1, TestHttpClient1>()
            .AddSingleton<ITestHttpClient2, TestHttpClient2>()
            .AddHttpClient<ITestHttpClient1, TestHttpClient1>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.LogRequestStart = true;
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test1", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient<ITestHttpClient2, TestHttpClient2>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.LogRequestStart = false;
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test2", FakeTaxonomy.PrivateData } };
            })
            .Services
            .BuildServiceProvider();

        var firstClient = provider.GetService<ITestHttpClient1>() as TestHttpClient1;
        var secondClient = provider.GetService<ITestHttpClient2>() as TestHttpClient2;

        firstClient.Should().NotBe(secondClient);

        var optionsFirst = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient1));
        var optionsSecond = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient2));
        optionsFirst.Should().NotBeNull();
        optionsSecond.Should().NotBeNull();
        optionsFirst.Should().NotBeEquivalentTo(optionsSecond);
    }

    [Fact]
    public void AddHttpClientLogging_WithTypedAndNamedClients_RegistersNamedOptions()
    {
        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddSingleton<ITestHttpClient1, TestHttpClient1>()
            .AddSingleton<ITestHttpClient2, TestHttpClient2>()
            .AddHttpClient<ITestHttpClient1, TestHttpClient1>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test1", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient<ITestHttpClient2, TestHttpClient2>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test2", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient("testClient3")
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test3", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient("testClient4")
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test4", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddHttpClient<ITestHttpClient1, TestHttpClient1>("testClient5")
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test5", FakeTaxonomy.PrivateData } };
            })
            .Services
            .AddExtendedHttpClientLogging(options =>
            {
                options.ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { "test6", FakeTaxonomy.PrivateData } };
            })
            .BuildServiceProvider();

        var optionsFirst = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient1));
        var optionsSecond = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient2));
        var optionsThird = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("testClient3");
        var optionsFourth = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("testClient4");
        var optionsFifth = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("testClient5");
        var optionsSixth = provider.GetRequiredService<IOptions<LoggingOptions>>().Value;

        optionsFirst.Should().NotBeNull();
        optionsSecond.Should().NotBeNull();
        optionsFirst.Should().NotBeEquivalentTo(optionsSecond);

        optionsThird.Should().NotBeNull();
        optionsFourth.Should().NotBeNull();
        optionsThird.Should().NotBeEquivalentTo(optionsFourth);

        optionsFifth.Should().NotBeNull();
        optionsFifth.Should().NotBeEquivalentTo(optionsFourth);

        optionsSixth.Should().NotBeNull();
        optionsSixth.Should().NotBeEquivalentTo(optionsFifth);
    }

    [Fact]
    public async Task AddHttpClientLogging_DisablesNetScope()
    {
        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient("test")
             .AddExtendedHttpClientLogging()
             .Services
             .BlockRemoteCall()
             .BuildServiceProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _unreachableRequestUri);

        _ = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var collector = provider.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);

        logRecord.Scopes.Should().BeEmpty();
    }

    [Fact]
    public async Task AddHttpClientLogging_CallFromOtherClient_HasBuiltInLogging()
    {
        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient("test")
             .AddExtendedHttpClientLogging()
             .Services
             .AddHttpClient("normal")
             .Services
             .BlockRemoteCall()
             .BuildServiceProvider();

        // The test client has AddHttpClientLogging. The normal client doesn't.
        // The normal client should still log via the built-in HTTP logging.
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("normal");
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _unreachableRequestUri);

        _ = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var collector = provider.GetFakeLogCollector();
        var logRecords = collector.GetSnapshot().Where(l => l.Category == "System.Net.Http.HttpClient.normal.LogicalHandler").ToList();

        Assert.Collection(logRecords,
            r => Assert.Equal("RequestPipelineStart", r.Id.Name),
            r => Assert.Equal("RequestPipelineEnd", r.Id.Name));
    }

    [Fact]
    public async Task AddDefaultHttpClientLogging_DisablesNetScope()
    {
        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient()
             .AddExtendedHttpClientLogging()
             .BlockRemoteCall()
             .BuildServiceProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _unreachableRequestUri);

        _ = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var collector = provider.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);

        logRecord.Scopes.Should().HaveCount(0);
    }

    [Theory]
    [InlineData(4_096)]
    [InlineData(8_192)]
    [InlineData(16_384)]
    [InlineData(32_768)]
    [InlineData(315_883)]
    public async Task HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit(int limit)
    {
        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient(nameof(HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit))
             .AddExtendedHttpClientLogging(x =>
             {
                 x.ResponseHeadersDataClasses.Add("ResponseHeader", FakeTaxonomy.PrivateData);
                 x.RequestHeadersDataClasses.Add("RequestHeader", FakeTaxonomy.PrivateData);
                 x.RequestHeadersDataClasses.Add("RequestHeader2", FakeTaxonomy.PrivateData);
                 x.RequestBodyContentTypes.Add("application/json");
                 x.ResponseBodyContentTypes.Add("application/json");
                 x.BodySizeLimit = limit;
                 x.LogBody = true;
             })
             .Services
             .BlockRemoteCall()
             .BuildServiceProvider();

        var client = provider
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit));

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = _unreachableRequestUri,
        };

        httpRequestMessage.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage.Headers.Add("ReQuEStHeAdEr2", new List<string> { "Request Value 2", "Request Value 3" });

        var content = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var responseStream = await content.Content.ReadAsStreamAsync();
        var length = (int)responseStream.Length > limit ? limit : (int)responseStream.Length;
        var buffer = new byte[length];
        _ = await responseStream.ReadAsync(buffer, 0, length);
        var responseString = Encoding.UTF8.GetString(buffer);

        var collector = provider.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(l => l.Category == LoggingCategory);
        var state = logRecord.StructuredState;
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");
    }
}

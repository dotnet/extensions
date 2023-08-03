// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Collections;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpLoggingHandlerTest
{
    private const string TestRequestHeader = "RequestHeader";
    private const string TestResponseHeader = "ResponseHeader";
    private const string TestExpectedRequestHeaderKey = $"{HttpClientLoggingTagNames.RequestHeaderPrefix}{TestRequestHeader}";
    private const string TestExpectedResponseHeaderKey = $"{HttpClientLoggingTagNames.ResponseHeaderPrefix}{TestResponseHeader}";

    private const string TextPlain = "text/plain";

    private const string Redacted = "REDACTED";

    private readonly Fixture _fixture;

    public HttpLoggingHandlerTest()
    {
        _fixture = new();
    }

    [Fact]
    public void HttpLoggingHandler_NullOptions_Throws()
    {
        var options = Microsoft.Extensions.Options.Options.Create<LoggingOptions>(null!);
        var act = () => new HttpLoggingHandler(
            NullLogger<HttpLoggingHandler>.Instance,
            Mock.Of<IHttpRequestReader>(),
            Empty.Enumerable<IHttpClientLogEnricher>(),
        options);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsException()
    {
        var responseCode = _fixture.Create<HttpStatusCode>();
        using var httpResponseMessage = new HttpResponseMessage { StatusCode = responseCode };
        using var client = HttpLoggingHandlerTest.CreateClient(httpResponseMessage);

        var act = async () =>
            await client.SendAsync(null!, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
    }

    [Fact]
    public async Task SendAsync_HttpRequestException_ThrowsException()
    {
        var input = _fixture.Create<string>();
        var exception = new HttpRequestException();
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("http://default-uri.com"),
            Content = new StringContent(input, Encoding.UTF8, TextPlain)
        };

        using var client = HttpLoggingHandlerTest.CreateClientWithException(exception, isLoggingEnabled: false);

        var act = async () =>
            await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        await act.Should().ThrowAsync<HttpRequestException>().Where(e => e == exception);
    }

    [Fact]
    public async Task SendAsync_ReadRequestAsyncThrowsOperationCancelled_ThrowsOperationCancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var input = _fixture.Create<string>();
        var operationCanceledException = new OperationCanceledException();
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("http://default-uri.com"),
            Content = new StringContent(input, Encoding.UTF8, TextPlain)
        };
        using var httpClient = HttpLoggingHandlerTest.CreateClientWithOperationCanceledException(operationCanceledException);

        var act = async () =>
            await httpClient.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HttpLoggingHandler_AllOptions_LogsOutgoingRequest()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var testRequestHeaderValue = _fixture.Create<string>();
        var testResponseHeaderValue = _fixture.Create<string>();

        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var testEnricher = new TestEnricher();

        var testSharedRequestHeaderKey = $"{HttpClientLoggingTagNames.RequestHeaderPrefix}Header3";
        var testSharedResponseHeaderKey = $"{HttpClientLoggingTagNames.ResponseHeaderPrefix}Header3";

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestExpectedResponseHeaderKey, Redacted), new(testSharedResponseHeaderKey, Redacted) },
            RequestHeaders = new() { new(TestExpectedRequestHeaderKey, Redacted), new(testSharedRequestHeaderKey, Redacted) },
            RequestBody = requestContent,
            ResponseBody = responseContent,
            EnrichmentTags = testEnricher.EnrichmentCollector
        };

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = new StringContent(requestContent, Encoding.UTF8, TextPlain)
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, testRequestHeaderValue);
        httpRequestMessage.Headers.Add("Header3", testRequestHeaderValue);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8, TextPlain),
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, testResponseHeaderValue);
        httpResponseMessage.Headers.Add("Header3", testRequestHeaderValue);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData }, { "Header3", SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData }, { "Header3", SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = false,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions())));

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            })
        };
        handler.TimeProvider = fakeTimeProvider;

        using var client = new System.Net.Http.HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingTagNames.ResponseBody, expectedLogRecord.ResponseBody);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders[0].Value);
        logRecord.Contains(TestExpectedResponseHeaderKey, expectedLogRecord.ResponseHeaders[0].Value);
        logRecord.Contains(testSharedResponseHeaderKey, expectedLogRecord.ResponseHeaders[1].Value);
        logRecord.Contains(testSharedRequestHeaderKey, expectedLogRecord.RequestHeaders[1].Value);
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
    }

    [Fact]
    public async Task HttpLoggingHandler_AllOptionsWithLogRequestStart_LogsOutgoingRequestWithTwoRecords()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var requestHeaderValue = _fixture.Create<string>();
        var responseHeaderValue = _fixture.Create<string>();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testEnricher = new TestEnricher();

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestResponseHeader, Redacted) },
            RequestHeaders = new() { new(TestRequestHeader, Redacted) },
            RequestBody = requestContent,
            ResponseBody = responseContent,
            EnrichmentTags = testEnricher.EnrichmentCollector
        };

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = new StringContent(requestContent, Encoding.UTF8, TextPlain)
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, requestHeaderValue);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8, TextPlain),
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, responseHeaderValue);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = true,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(
            new FakeLogCollector(
                Microsoft.Extensions.Options.Options.Create(
                    new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor
            .Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            })
        };
        handler.TimeProvider = fakeTimeProvider;

        using var client = new System.Net.Http.HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        var logRecordRequest = logRecords[0].GetStructuredState();
        logRecordRequest.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecordRequest.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecordRequest.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecordRequest.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecordRequest.NotContains(HttpClientLoggingTagNames.StatusCode);
        logRecordRequest.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecordRequest.NotContains(testEnricher.KvpRequest.Key);

        var logRecordFull = logRecords[1].GetStructuredState();
        logRecordFull.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecordFull.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecordFull.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecordFull.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecordFull.Contains(HttpClientLoggingTagNames.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecordFull.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecordFull.Contains(HttpClientLoggingTagNames.ResponseBody, expectedLogRecord.ResponseBody);
        logRecordFull.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecordFull.Contains(TestExpectedResponseHeaderKey, expectedLogRecord.ResponseHeaders.FirstOrDefault().Value);
        logRecordFull.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
        logRecordFull.Contains(testEnricher.KvpResponse.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpResponse.Key));
    }

    [Fact]
    public async Task HttpLoggingHandler_AllOptionsSendAsyncFailed_LogsRequestInformation()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var requestHeaderValue = _fixture.Create<string>();
        var responseHeaderValue = _fixture.Create<string>();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testEnricher = new TestEnricher();

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestResponseHeader, Redacted) },
            RequestHeaders = new() { new(TestRequestHeader, Redacted) },
            RequestBody = requestContent,
            ResponseBody = responseContent,
            EnrichmentTags = testEnricher.EnrichmentCollector
        };

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = new StringContent(requestContent, Encoding.UTF8, TextPlain)
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, requestHeaderValue);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8, TextPlain),
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, responseHeaderValue);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = false,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions())));

        var exception = new OperationCanceledException();

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(expectedLogRecord.Duration));
                throw exception;
            }),
            TimeProvider = fakeTimeProvider
        };

        using var client = new System.Net.Http.HttpClient(handler);
        var act = async () => await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);
        await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);
        logRecords[0].Message.Should().BeEmpty();
        logRecords[0].Exception.Should().Be(exception);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecord.NotContains(HttpClientLoggingTagNames.ResponseBody);
        logRecord.NotContains(HttpClientLoggingTagNames.StatusCode);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Should().NotContain(kvp => kvp.Key.StartsWith(HttpClientLoggingTagNames.ResponseHeaderPrefix));
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
        logRecord.NotContains(testEnricher.KvpResponse.Key);
        logRecord.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
    }

    [Fact(Skip = "Flaky test, see https://github.com/dotnet/r9/issues/372")]
    public async Task HttpLoggingHandler_ReadResponseThrows_LogsException()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var requestHeaderValue = _fixture.Create<string>();
        var responseHeaderValue = _fixture.Create<string>();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testEnricher = new TestEnricher();

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestResponseHeader, Redacted) },
            RequestHeaders = new() { new(TestRequestHeader, Redacted) },
            RequestBody = requestContent,
            ResponseBody = responseContent,
            EnrichmentTags = testEnricher.EnrichmentCollector
        };

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = new StringContent(requestContent, Encoding.UTF8, TextPlain)
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, requestHeaderValue);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8, TextPlain),
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, responseHeaderValue);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = false,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(
            new FakeLogCollector(
                Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor
            .Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        var exception = new InvalidOperationException("test");

        var actualRequestReader = new HttpRequestReader(options, GetHttpRouteFormatter(), headersReader, RequestMetadataContext);
        var mockedRequestReader = new Mock<IHttpRequestReader>();
        mockedRequestReader
            .Setup(m =>
                m.ReadRequestAsync(// so this method is not mocked
                    It.IsAny<LogRecord>(),
                    It.IsAny<HttpRequestMessage>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns((LogRecord a, HttpRequestMessage b, List<KeyValuePair<string, string>> c, CancellationToken d) =>
                actualRequestReader.ReadRequestAsync(a, b, c, d));
        mockedRequestReader
            .Setup(m =>
                m.ReadResponseAsync(// but this method is setup to throw an exception
                    It.IsAny<LogRecord>(),
                    It.IsAny<HttpResponseMessage>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            mockedRequestReader.Object,
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            })
        };

        handler.TimeProvider = fakeTimeProvider;

        using var client = new System.Net.Http.HttpClient(handler);
        var act = async () => await client
            .SendAsync(httpRequestMessage, It.IsAny<CancellationToken>())
            .ConfigureAwait(false);
        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);
        logRecords[0].Exception.Should().Be(exception);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecord.NotContains(HttpClientLoggingTagNames.ResponseBody);
        logRecord.NotContains(HttpClientLoggingTagNames.StatusCode);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Should().NotContain(kvp => kvp.Key.StartsWith(HttpClientLoggingTagNames.ResponseHeaderPrefix));
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
        logRecord.Contains(testEnricher.KvpResponse.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpResponse.Key));
        logRecord.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HttpLoggingHandler_AllOptionsTransferEncodingIsNotChunked_LogsOutgoingRequest()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var requestHeaderValue = _fixture.Create<string>();
        var responseHeaderValue = _fixture.Create<string>();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testEnricher = new TestEnricher();

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestResponseHeader, Redacted) },
            RequestHeaders = new() { new(TestRequestHeader, Redacted) },
            RequestBody = requestContent,
            ResponseBody = responseContent,
            EnrichmentTags = testEnricher.EnrichmentCollector,
        };

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = new StringContent(requestContent, Encoding.UTF8, TextPlain)
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, requestHeaderValue);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8, TextPlain),
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, responseHeaderValue);
        httpResponseMessage.Headers.TransferEncoding.Add(new("compress"));

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = false,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            })
        };
        handler.TimeProvider = fakeTimeProvider;

        using var client = new System.Net.Http.HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingTagNames.ResponseBody, expectedLogRecord.ResponseBody);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Contains(TestExpectedResponseHeaderKey, expectedLogRecord.ResponseHeaders.FirstOrDefault().Value);
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_CallsEnrichMethodExactlyOnce()
    {
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var fakeLogger = new FakeLogger<HttpLoggingHandler>();
        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                new HttpHeadersReader(options, mockHeadersRedactor.Object), RequestMetadataContext),
            new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichersAndLogRequestStart_CallsEnrichMethodExactlyOnce()
    {
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            LogRequestStart = true
        });
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var fakeLogger = new FakeLogger<HttpLoggingHandler>();
        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                new HttpHeadersReader(options, mockHeadersRedactor.Object), RequestMetadataContext),
            new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_OneEnricherThrows_LogsEnrichmentErrorAndRequest()
    {
        var exception = new ArgumentNullException();
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        enricher1
            .Setup(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()))
            .Throws(exception);

        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var fakeLogger = new FakeLogger<HttpLoggingHandler>();
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());
        var headersReader = new HttpHeadersReader(options, new Mock<IHttpHeadersRedactor>().Object);
        var requestReader = new HttpRequestReader(options, GetHttpRouteFormatter(), headersReader, RequestMetadataContext);
        var enrichers = new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object };

        using var handler = new HttpLoggingHandler(fakeLogger, requestReader, enrichers, options)
        {
            InnerHandler = new TestingHandlerStub(
                (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        Assert.Equal(nameof(Log.EnrichmentError), logRecords[0].Id.Name);
        Assert.Equal(exception, logRecords[0].Exception);

        Assert.Equal(nameof(Log.OutgoingRequest), logRecords[1].Id.Name);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_SendAsyncAndOneEnricher_LogsEnrichmentErrorAndRequestError()
    {
        var enrichmentException = new ArgumentNullException();
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        enricher1
            .Setup(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()))
            .Throws(enrichmentException)
            .Verifiable();

        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var fakeLogger = new FakeLogger<HttpLoggingHandler>();

        var sendAsyncException = new OperationCanceledException();
        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new Mock<IHttpRequestReader>().Object,
            new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
            Microsoft.Extensions.Options.Options.Create(new LoggingOptions()))
        {
            InnerHandler = new TestingHandlerStub((_, _) => throw sendAsyncException)
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        var act = () => client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>());
        await act.Should().ThrowAsync<OperationCanceledException>();

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        Assert.Equal(nameof(Log.EnrichmentError), logRecords[0].Id.Name);
        Assert.Equal(enrichmentException, logRecords[0].Exception);

        Assert.Equal(nameof(Log.OutgoingRequestError), logRecords[1].Id.Name);
        Assert.Equal(sendAsyncException, logRecords[1].Exception);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentTagCollector>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_AllOptionsTransferEncodingChunked_LogsOutgoingRequest()
    {
        var requestInput = _fixture.Create<string>();
        var responseInput = _fixture.Create<string>();
        var requestHeaderValue = _fixture.Create<string>();
        var responseHeaderValue = _fixture.Create<string>();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testEnricher = new TestEnricher();

        var expectedLogRecord = new LogRecord
        {
            Host = "default-uri.com",
            Method = HttpMethod.Post,
            Path = "foo/bar",
            Duration = 1000,
            StatusCode = 200,
            ResponseHeaders = new() { new(TestExpectedResponseHeaderKey, Redacted) },
            RequestHeaders = new() { new(TestExpectedRequestHeaderKey, Redacted) },
            RequestBody = requestInput,
            ResponseBody = responseInput,
            EnrichmentTags = testEnricher.EnrichmentCollector
        };

        using var requestContent = new StreamContent(new NotSeekableStream(new(Encoding.UTF8.GetBytes(requestInput))));
        requestContent.Headers.Add("Content-Type", TextPlain);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://{expectedLogRecord.Host}/{expectedLogRecord.Path}"),
            Content = requestContent,
        };
        httpRequestMessage.Headers.Add(TestRequestHeader, requestHeaderValue);

        using var responseContent = new StreamContent(new NotSeekableStream(new(Encoding.UTF8.GetBytes(responseInput))));
        responseContent.Headers.Add("Content-Type", TextPlain);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = responseContent,
        };
        httpResponseMessage.Headers.Add(TestResponseHeader, responseHeaderValue);
        httpResponseMessage.Headers.TransferEncoding.Add(new("chunked"));

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { TestResponseHeader, SimpleClassifications.PrivateData } },
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { TestRequestHeader, SimpleClassifications.PrivateData } },
            ResponseBodyContentTypes = new HashSet<string> { TextPlain },
            RequestBodyContentTypes = new HashSet<string> { TextPlain },
            BodySizeLimit = 32000,
            BodyReadTimeout = TimeSpan.FromMinutes(5),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured,
            LogRequestStart = false,
            LogBody = true,
            RouteParameterDataClasses = { { "userId", SimpleClassifications.PrivateData } },
        });

        var fakeLogger = new FakeLogger<HttpLoggingHandler>(new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new HttpLoggingHandler(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            })
        };
        handler.TimeProvider = fakeTimeProvider;

        using var client = new System.Net.Http.HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingTagNames.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingTagNames.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingTagNames.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingTagNames.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingTagNames.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingTagNames.ResponseBody, expectedLogRecord.ResponseBody);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Contains(TestExpectedResponseHeaderKey, expectedLogRecord.ResponseHeaders.FirstOrDefault().Value);
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
    }

    [Theory]
    [InlineData(399, LogLevel.Information)]
    [InlineData(400, LogLevel.Error)]
    [InlineData(499, LogLevel.Error)]
    [InlineData(500, LogLevel.Error)]
    [InlineData(599, LogLevel.Error)]
    [InlineData(600, LogLevel.Information)]
    public async Task HttpLoggingHandler_OnDifferentHttpStatusCodes_LogsOutgoingRequestWithAppropriateLogLevel(
        int httpStatusCode, LogLevel expectedLogLevel)
    {
        var fakeLogger = new FakeLogger<HttpLoggingHandler>(new FakeLogCollector());
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());
        var headersReader = new HttpHeadersReader(options, new Mock<IHttpHeadersRedactor>().Object);
        var requestReader = new HttpRequestReader(
            options, GetHttpRouteFormatter(), headersReader, RequestMetadataContext);

        using var handler = new HttpLoggingHandler(
            fakeLogger, requestReader, new List<IHttpClientLogEnricher>(), options)
        {
            InnerHandler = new TestingHandlerStub((_, _) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)httpStatusCode)))
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent("request_content", Encoding.UTF8, TextPlain)
        };
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecord = fakeLogger.Collector.GetSnapshot().Single();
        Assert.Equal(expectedLogLevel, logRecord.Level);
    }

    private static System.Net.Http.HttpClient CreateClientWithException(
        Exception exception,
        bool isLoggingEnabled = true)
    {
        var loggerMock = new Mock<ILogger<HttpLoggingHandler>>(MockBehavior.Strict);
        loggerMock.Setup(m => m.IsEnabled(It.IsAny<LogLevel>())).Returns(isLoggingEnabled);
        var mockedLogger = new MockedLogger<HttpLoggingHandler>(loggerMock);

        var mockHandler = new Mock<DelegatingHandler>();
        mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()).Throws(exception);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

#pragma warning disable CA2000 // Dispose objects before losing scope - no, it is required for the HttpClient to work properly
        var handler = new HttpLoggingHandler(
            mockedLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(), headersReader, RequestMetadataContext),
            Enumerable.Empty<IHttpClientLogEnricher>(),
            options)
        {
            InnerHandler = mockHandler.Object
        };
#pragma warning restore CA2000 // Dispose objects before losing scope

        var client = new System.Net.Http.HttpClient(handler);
        return client;
    }

    private static System.Net.Http.HttpClient CreateClientWithOperationCanceledException(
        Exception exception,
        bool isLoggingEnabled = true)
    {
        var requestReaderMock = new Mock<IHttpRequestReader>(MockBehavior.Strict);
        requestReaderMock.Setup(e =>
            e.ReadRequestAsync(It.IsAny<LogRecord>(),
                It.IsAny<HttpRequestMessage>(),
It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<CancellationToken>())).Throws(exception);
        var mockedRequestReader = new MockedRequestReader(requestReaderMock);

        var loggerMock = new Mock<ILogger<HttpLoggingHandler>>();
        loggerMock.Setup(m => m.IsEnabled(It.IsAny<LogLevel>())).Returns(isLoggingEnabled);
        var mockedLogger = new MockedLogger<HttpLoggingHandler>(loggerMock);

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());

        using var handler = new HttpLoggingHandler(
            mockedLogger,
            mockedRequestReader,
            Enumerable.Empty<IHttpClientLogEnricher>(),
            options);

        var client = new System.Net.Http.HttpClient(handler);
        return client;
    }

    private static System.Net.Http.HttpClient CreateClient(
        HttpResponseMessage httpResponseMessage,
        LogLevel logLevel = LogLevel.Information,
        bool isLoggingEnabled = true,
        Action<LoggingOptions>? setupOptions = null)
    {
        var options = new LoggingOptions
        {
            BodyReadTimeout = TimeSpan.FromMinutes(5)
        };

        var loggerMock = new Mock<ILogger<HttpLoggingHandler>>(MockBehavior.Strict);
        loggerMock.Setup(m => m.IsEnabled(logLevel)).Returns(isLoggingEnabled);
        var logger = new MockedLogger<HttpLoggingHandler>(loggerMock);

        setupOptions?.Invoke(options);

        using var handler = new HttpLoggingHandler(
            logger, Mock.Of<IHttpRequestReader>(),
            Empty.Enumerable<IHttpClientLogEnricher>(),
        Microsoft.Extensions.Options.Options.Create(options))
        {
            InnerHandler = new TestingHandlerStub((_, _) => Task.FromResult(httpResponseMessage))
        };

        var client = new System.Net.Http.HttpClient(handler);
        return client;
    }

    private static IHttpRouteFormatter GetHttpRouteFormatter()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpRouteProcessor()
            .BuildServiceProvider();

        return builder.GetService<IHttpRouteFormatter>()!;
    }

    private static IOutgoingRequestContext RequestMetadataContext => new Mock<IOutgoingRequestContext>().Object;
}

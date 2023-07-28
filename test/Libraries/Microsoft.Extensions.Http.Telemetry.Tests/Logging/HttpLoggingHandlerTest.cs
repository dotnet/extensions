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
using Microsoft.Extensions.Http.Telemetry.Test.Logging.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Collections;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpLoggingHandlerTest
{
    private const string TestRequestHeader = "RequestHeader";
    private const string TestResponseHeader = "ResponseHeader";
    private const string TestExpectedRequestHeaderKey = $"{HttpClientLoggingDimensions.RequestHeaderPrefix}{TestRequestHeader}";
    private const string TestExpectedResponseHeaderKey = $"{HttpClientLoggingDimensions.ResponseHeaderPrefix}{TestResponseHeader}";

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
        var options = Options.Options.Create<LoggingOptions>(null!);
        var act = () => new HttpClientLogger(
            NullLogger<HttpClientLogger>.Instance,
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

        var options = new LoggingOptions
        {
            BodyReadTimeout = TimeSpan.FromMinutes(5)
        };

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                NullLogger<HttpClientLogger>.Instance,
                Mock.Of<IHttpRequestReader>(),
                Empty.Enumerable<IHttpClientLogEnricher>(),
                Options.Options.Create(options)),
            new TestingHandlerStub((_, _) => Task.FromResult(httpResponseMessage)));

        using var client = new HttpClient(handler);

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

        var options = Options.Options.Create(new LoggingOptions());

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                NullLogger<HttpClientLogger>.Instance,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(), headersReader, RequestMetadataContext),
                Enumerable.Empty<IHttpClientLogEnricher>(),
                options),
            new TestingHandlerStub((_, _) => throw exception));

        using var client = new HttpClient(handler);

        var act = async () =>
            await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        await act.Should().ThrowAsync<HttpRequestException>().Where(e => e == exception);
    }

    [Fact]
    public async Task Logger_WhenReadRequestAsyncThrows_DoesntThrow()
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

        var requestReaderMock = new Mock<IHttpRequestReader>(MockBehavior.Strict);
        requestReaderMock.Setup(e =>
            e.ReadRequestAsync(It.IsAny<LogRecord>(),
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<CancellationToken>()))
            .Throws(operationCanceledException);

        var mockedRequestReader = new MockedRequestReader(requestReaderMock);

        var options = Options.Options.Create(new LoggingOptions());

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                NullLogger<HttpClientLogger>.Instance,
                mockedRequestReader,
                Enumerable.Empty<IHttpClientLogEnricher>(),
                options),
            new TestingHandlerStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        using var httpClient = new HttpClient(handler);

        var act = async () =>
            await httpClient.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        await act.Should().NotThrowAsync<Exception>();
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

        var testSharedRequestHeaderKey = $"{HttpClientLoggingDimensions.RequestHeaderPrefix}Header3";
        var testSharedResponseHeaderKey = $"{HttpClientLoggingDimensions.ResponseHeaderPrefix}Header3";

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
            EnrichmentProperties = testEnricher.EnrichmentBag
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(new FakeLogCollector(Options.Options.Create(new FakeLogCollectorOptions())));

        var logger = new HttpClientLogger(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options);

        using var handler = new TestLoggingHandler(
            logger,
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            }));

        using var client = new HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingDimensions.ResponseBody, expectedLogRecord.ResponseBody);
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
            EnrichmentProperties = testEnricher.EnrichmentBag
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(
            new FakeLogCollector(
                Options.Options.Create(
                    new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor
            .Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        var logger = new HttpClientLogger(
            fakeLogger,
            new HttpRequestReader(
                options,
                GetHttpRouteFormatter(),
                headersReader, RequestMetadataContext),
            new List<IHttpClientLogEnricher> { testEnricher },
            options);

        using var handler = new TestLoggingHandler(
            logger,
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            }));

        using var client = new HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        var logRecordRequest = logRecords[0].GetStructuredState();
        logRecordRequest.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecordRequest.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecordRequest.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecordRequest.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecordRequest.NotContains(HttpClientLoggingDimensions.StatusCode);
        logRecordRequest.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecordRequest.NotContains(testEnricher.KvpRequest.Key);

        var logRecordFull = logRecords[1].GetStructuredState();
        logRecordFull.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecordFull.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecordFull.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecordFull.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecordFull.Contains(HttpClientLoggingDimensions.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecordFull.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecordFull.Contains(HttpClientLoggingDimensions.ResponseBody, expectedLogRecord.ResponseBody);
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
            EnrichmentProperties = testEnricher.EnrichmentBag
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(new FakeLogCollector(Options.Options.Create(new FakeLogCollectorOptions())));

        var exception = new OperationCanceledException();

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(),
                    headersReader, RequestMetadataContext),
                new List<IHttpClientLogEnricher> { testEnricher },
                options),
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(expectedLogRecord.Duration));
                throw exception;
            }));

        using var client = new HttpClient(handler);
        var act = async () => await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);
        await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);
        logRecords[0].Message.Should().Be($"{httpRequestMessage.Method} {httpRequestMessage.RequestUri.Host}/{TelemetryConstants.Redacted}");
        logRecords[0].Exception.Should().Be(exception);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecord.NotContains(HttpClientLoggingDimensions.ResponseBody);
        logRecord.NotContains(HttpClientLoggingDimensions.StatusCode);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Should().NotContain(kvp => kvp.Key.StartsWith(HttpClientLoggingDimensions.ResponseHeaderPrefix));
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
        logRecord.NotContains(testEnricher.KvpResponse.Key);
        logRecord.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
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
            EnrichmentProperties = testEnricher.EnrichmentBag
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(
            new FakeLogCollector(
                Options.Options.Create(new FakeLogCollectorOptions())));

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

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                mockedRequestReader.Object,
                new List<IHttpClientLogEnricher> { testEnricher },
                options),
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            }));

        using var client = new HttpClient(handler);
        var act = async () => await client
            .SendAsync(httpRequestMessage, It.IsAny<CancellationToken>())
            .ConfigureAwait(false);
        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);
        logRecords[0].Exception.Should().Be(exception);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecord.NotContains(HttpClientLoggingDimensions.ResponseBody);
        logRecord.NotContains(HttpClientLoggingDimensions.StatusCode);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Should().NotContain(kvp => kvp.Key.StartsWith(HttpClientLoggingDimensions.ResponseHeaderPrefix));
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
        logRecord.Contains(testEnricher.KvpResponse.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpResponse.Key));
        logRecord.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
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
            EnrichmentProperties = testEnricher.EnrichmentBag,
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(new FakeLogCollector(Options.Options.Create(new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(),
                    headersReader, RequestMetadataContext),
                new List<IHttpClientLogEnricher> { testEnricher },
                options),
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            }));

        using var client = new HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingDimensions.ResponseBody, expectedLogRecord.ResponseBody);
        logRecord.Contains(TestExpectedRequestHeaderKey, expectedLogRecord.RequestHeaders.FirstOrDefault().Value);
        logRecord.Contains(TestExpectedResponseHeaderKey, expectedLogRecord.ResponseHeaders.FirstOrDefault().Value);
        logRecord.Contains(testEnricher.KvpRequest.Key, expectedLogRecord.GetEnrichmentProperty(testEnricher.KvpRequest.Key));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_CallsEnrichMethodExactlyOnce()
    {
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var options = Options.Options.Create(new LoggingOptions());
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var fakeLogger = new FakeLogger<HttpClientLogger>();
        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(),
                    new HttpHeadersReader(options, mockHeadersRedactor.Object), RequestMetadataContext),
                new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
                options),
            new TestingHandlerStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        using var client = new HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichersAndLogRequestStart_CallsEnrichMethodExactlyOnce()
    {
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var options = Options.Options.Create(new LoggingOptions
        {
            LogRequestStart = true
        });
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var fakeLogger = new FakeLogger<HttpClientLogger>();
        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(),
                    new HttpHeadersReader(options, mockHeadersRedactor.Object), RequestMetadataContext),
                new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
                options),
            new TestingHandlerStub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        using var client = new HttpClient(handler);
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new($"http://default-uri.com/foo/bar"),
            Content = new StringContent(_fixture.Create<string>(), Encoding.UTF8, TextPlain)
        };

        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(2);

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_OneEnricherThrows_LogsEnrichmentErrorAndRequest()
    {
        var exception = new ArgumentNullException();
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        enricher1
            .Setup(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()))
            .Throws(exception);

        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var fakeLogger = new FakeLogger<HttpClientLogger>();
        var options = Options.Options.Create(new LoggingOptions());
        var headersReader = new HttpHeadersReader(options, new Mock<IHttpHeadersRedactor>().Object);
        var requestReader = new HttpRequestReader(options, GetHttpRouteFormatter(), headersReader, RequestMetadataContext);
        var enrichers = new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object };

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(fakeLogger, requestReader, enrichers, options),
            new TestingHandlerStub(
                (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        using var client = new HttpClient(handler);
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

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HttpLoggingHandler_WithEnrichers_SendAsyncAndOneEnricher_LogsEnrichmentErrorAndRequestError()
    {
        var enrichmentException = new ArgumentNullException();
        var enricher1 = new Mock<IHttpClientLogEnricher>();
        enricher1
            .Setup(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()))
            .Throws(enrichmentException)
            .Verifiable();

        var enricher2 = new Mock<IHttpClientLogEnricher>();
        var fakeLogger = new FakeLogger<HttpClientLogger>();

        var sendAsyncException = new OperationCanceledException();
        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new Mock<IHttpRequestReader>().Object,
                new List<IHttpClientLogEnricher> { enricher1.Object, enricher2.Object },
                Options.Options.Create(new LoggingOptions())),
            new TestingHandlerStub((_, _) => throw sendAsyncException));

        using var client = new HttpClient(handler);
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

        enricher1.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
        enricher2.Verify(e => e.Enrich(It.IsAny<IEnrichmentPropertyBag>(), It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<Exception>()), Times.Exactly(1));
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
            EnrichmentProperties = testEnricher.EnrichmentBag
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

        var options = Options.Options.Create(new LoggingOptions
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

        var fakeLogger = new FakeLogger<HttpClientLogger>(new FakeLogCollector(Options.Options.Create(new FakeLogCollectorOptions())));

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);
        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(
                fakeLogger,
                new HttpRequestReader(
                    options,
                    GetHttpRouteFormatter(),
                    headersReader, RequestMetadataContext),
                new List<IHttpClientLogEnricher> { testEnricher },
                options),
            new TestingHandlerStub((_, _) =>
            {
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(1000));
                return Task.FromResult(httpResponseMessage);
            }));

        using var client = new HttpClient(handler);
        await client.SendAsync(httpRequestMessage, It.IsAny<CancellationToken>()).ConfigureAwait(false);

        var logRecords = fakeLogger.Collector.GetSnapshot();
        logRecords.Count.Should().Be(1);

        var logRecord = logRecords[0].GetStructuredState();
        logRecord.Contains(HttpClientLoggingDimensions.Host, expectedLogRecord.Host);
        logRecord.Contains(HttpClientLoggingDimensions.Method, expectedLogRecord.Method.ToString());
        logRecord.Contains(HttpClientLoggingDimensions.Path, TelemetryConstants.Redacted);
        logRecord.Contains(HttpClientLoggingDimensions.Duration, expectedLogRecord.Duration.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.StatusCode, expectedLogRecord.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
        logRecord.Contains(HttpClientLoggingDimensions.RequestBody, expectedLogRecord.RequestBody);
        logRecord.Contains(HttpClientLoggingDimensions.ResponseBody, expectedLogRecord.ResponseBody);
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
        var fakeLogger = new FakeLogger<HttpClientLogger>(new FakeLogCollector());
        var options = Options.Options.Create(new LoggingOptions());
        var headersReader = new HttpHeadersReader(options, new Mock<IHttpHeadersRedactor>().Object);
        var requestReader = new HttpRequestReader(
            options, GetHttpRouteFormatter(), headersReader, RequestMetadataContext);

        using var handler = new TestLoggingHandler(
            new HttpClientLogger(fakeLogger, requestReader, Array.Empty<IHttpClientLogEnricher>(), options),
            new TestingHandlerStub((_, _) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)httpStatusCode))));

        using var client = new HttpClient(handler);
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

    private static IHttpRouteFormatter GetHttpRouteFormatter()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpRouteProcessor()
            .BuildServiceProvider();

        return builder.GetService<IHttpRouteFormatter>()!;
    }

    private static IOutgoingRequestContext RequestMetadataContext
        => new Mock<IOutgoingRequestContext>().Object;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Http.Logging.Test.Internal;
using Microsoft.Extensions.Telemetry.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class HttpRequestReaderTest
{
    private const string Redacted = "REDACTED";

    private readonly Fixture _fixture;

    public HttpRequestReaderTest()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ReadAsync_AllData_ReturnsLogRecord()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");
        var header3 = new KeyValuePair<string, string>("Header3", "Value3");
        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = TelemetryConstants.Redacted,
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted), new("Header3", Redacted)],
            ResponseHeaders = [new("Header2", Redacted), new("Header3", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var options = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData }, { header3.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData }, { header3.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(100000),
            LogBody = true,
        };

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var serviceKey = "my-key";
        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(serviceKey), mockHeadersRedactor.Object, serviceKey);
        using var serviceProvider = GetServiceProvider(headersReader, serviceKey);

        var reader = new HttpRequestReader(serviceProvider, options.ToOptionsMonitor(serviceKey), serviceProvider.GetRequiredService<IHttpRouteFormatter>(),
            RequestMetadataContext, serviceKey: serviceKey);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo"),
            Content = new StringContent(requestContent, Encoding.UTF8)
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);
        httpRequestMessage.Headers.Add(header3.Key, header3.Value);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);
        httpResponseMessage.Headers.Add(header3.Key, header3.Value);

        var logRecord = new LogRecord();
        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        await reader.ReadRequestAsync(logRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(logRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        logRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());

        logRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        logRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_NoHost_ReturnsLogRecordWithoutHost()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        const string PlainTextMedia = "text/plain";

        var expectedRecord = new LogRecord
        {
            Host = TelemetryConstants.Unknown,
            Method = HttpMethod.Post,
            Path = TelemetryConstants.Unknown,
            StatusCode = 200,
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var options = new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { PlainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { PlainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            LogBody = true,
        };

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, options.ToOptionsMonitor(), serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = null,
            Content = new StringContent(requestContent, Encoding.UTF8)
        };

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        var actualRecord = new LogRecord();
        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());

        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_AllDataWithRequestMetadataSet_ReturnsLogRecord()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = "foo/bar/123",
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted)],
            ResponseHeaders = [new("Header2", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var opts = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            LogBody = true,
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "/foo/bar/{userId}"
        });

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());
        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_FormatRequestPathDisabled_ReturnsLogRecordWithRoute()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = "foo/bar/{userId}",
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted)],
            ResponseHeaders = [new("Header2", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var opts = new LoggingOptions
        {
            LogRequestStart = true,
            LogBody = true,
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestRoute = "foo/bar/{userId}"
        });

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());
        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_RouteParameterRedactionModeNone_ReturnsLogRecordWithUnredactedRoute()
    {
        var requestContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = "/foo/bar/123",
            RequestHeaders = [new("Header1", Redacted)],
            RequestBody = requestContent,
        };

        var opts = new LoggingOptions
        {
            LogRequestStart = true,
            LogBody = true,
            RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None,
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            RequestPathLoggingMode = OutgoingPathLoggingMode.Structured
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());
    }

    [Fact]
    public async Task ReadAsync_RequestMetadataRequestNameSetAndRouteMissing_ReturnsLogRecord()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = "TestRequest",
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted)],
            ResponseHeaders = [new("Header2", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var opts = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            LogBody = true,
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            RequestName = "TestRequest"
        });

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());

        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_NoMetadataUsesRedactedString_ReturnsLogRecord()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = TelemetryConstants.Redacted,
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted)],
            ResponseHeaders = [new("Header2", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var opts = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            LogBody = true,
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());
        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    [Fact]
    public async Task ReadAsync_MetadataWithoutRequestRouteOrNameUsesConstants_ReturnsLogRecord()
    {
        var requestContent = _fixture.Create<string>();
        var responseContent = _fixture.Create<string>();
        var host = "default-uri.com";
        var plainTextMedia = "text/plain";
        var header1 = new KeyValuePair<string, string>("Header1", "Value1");
        var header2 = new KeyValuePair<string, string>("Header2", "Value2");

        var expectedRecord = new LogRecord
        {
            Host = host,
            Method = HttpMethod.Post,
            Path = TelemetryConstants.Unknown,
            StatusCode = 200,
            RequestHeaders = [new("Header1", Redacted)],
            ResponseHeaders = [new("Header2", Redacted)],
            RequestBody = requestContent,
            ResponseBody = responseContent,
        };

        var opts = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification> { { header1.Key, FakeClassifications.PrivateData } },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification> { { header2.Key, FakeClassifications.PrivateData } },
            RequestBodyContentTypes = new HashSet<string> { plainTextMedia },
            ResponseBodyContentTypes = new HashSet<string> { plainTextMedia },
            BodyReadTimeout = TimeSpan.FromSeconds(10),
            LogBody = true,
        };

        opts.RouteParameterDataClasses.Add("userId", FakeClassifications.PrivateData);
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), It.IsAny<DataClassification>()))
            .Returns(Redacted);

        var headersReader = new HttpHeadersReader(opts.ToOptionsMonitor(), mockHeadersRedactor.Object);
        using var serviceProvider = GetServiceProvider(headersReader);

        var reader = new HttpRequestReader(serviceProvider, opts.ToOptionsMonitor(),
            serviceProvider.GetRequiredService<IHttpRouteFormatter>(), RequestMetadataContext);

        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://default-uri.com/foo/bar/123"),
            Content = new StringContent(requestContent, Encoding.UTF8),
        };

        httpRequestMessage.Headers.Add(header1.Key, header1.Value);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata());

        using var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseContent, Encoding.UTF8)
        };

        httpResponseMessage.Headers.Add(header2.Key, header2.Value);

        var requestHeadersBuffer = new List<KeyValuePair<string, string>>();
        var responseHeadersBuffer = new List<KeyValuePair<string, string>>();
        var actualRecord = new LogRecord();
        await reader.ReadRequestAsync(actualRecord, httpRequestMessage, requestHeadersBuffer, CancellationToken.None);
        await reader.ReadResponseAsync(actualRecord, httpResponseMessage, responseHeadersBuffer, CancellationToken.None);

        actualRecord.Should().BeEquivalentTo(
            expectedRecord,
            o => o
                .Excluding(m => m.RequestBody)
                .Excluding(m => m.ResponseBody)
                .ComparingByMembers<LogRecord>());

        actualRecord.RequestBody.Should().BeEquivalentTo(expectedRecord.RequestBody);
        actualRecord.ResponseBody.Should().BeEquivalentTo(expectedRecord.ResponseBody);
    }

    private static ServiceProvider GetServiceProvider(HttpHeadersReader headersReader, string? serviceKey = null)
    {
        var services = new ServiceCollection();
        if (serviceKey is null)
        {
            _ = services.AddSingleton<IHttpHeadersReader>(headersReader);
        }
        else
        {
            _ = services.AddKeyedSingleton<IHttpHeadersReader>(serviceKey, headersReader);
        }

        return services
            .AddFakeRedaction()
            .AddHttpRouteProcessor()
            .BuildServiceProvider();
    }

    private static IOutgoingRequestContext RequestMetadataContext => Mock.Of<IOutgoingRequestContext>();
}

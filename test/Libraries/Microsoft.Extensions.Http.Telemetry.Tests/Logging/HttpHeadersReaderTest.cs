// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Telemetry.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpHeadersReaderTest
{
    [Fact]
    public void HttpHeadersReader_WhenAnyArgumentIsNull_Throws()
    {
        var options = Microsoft.Extensions.Options.Options.Create((LoggingOptions)null!);
        var act = () => new HttpHeadersReader(options, Mock.Of<IHttpHeadersRedactor>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HttpHeadersReader_WhenEmptyHeaders_DoesNothing()
    {
        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());

        var headersReader = new HttpHeadersReader(options, Mock.Of<IHttpHeadersRedactor>());
        var buffer = new List<KeyValuePair<string, string>>();

        headersReader.ReadRequestHeaders(httpRequest, buffer);
        buffer.Should().BeEmpty();

        headersReader.ReadResponseHeaders(httpResponse, buffer);
        buffer.Should().BeEmpty();
    }

    [Fact]
    public void HttpHeadersReader_WhenHeadersProvided_ReadsThem()
    {
        const string Redacted = "REDACTED";
        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), SimpleClassifications.PrivateData))
            .Returns(Redacted);
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), SimpleClassifications.PublicData))
            .Returns<IEnumerable<string>, DataClassification>((x, _) => string.Join(",", x));

        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header1", SimpleClassifications.PrivateData },
                { "Header2", SimpleClassifications.PrivateData }
            },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header3", SimpleClassifications.PublicData },
                { "Header4", SimpleClassifications.PublicData },
                { "hEaDeR7", SimpleClassifications.PrivateData }
            },
        });

        var headersReader = new HttpHeadersReader(options, mockHeadersRedactor.Object);
        var buffer = new List<KeyValuePair<string, string>>();

        headersReader.ReadRequestHeaders(httpRequest, buffer);
        buffer.Should().BeEmpty();

        headersReader.ReadResponseHeaders(httpResponse, buffer);
        buffer.Should().BeEmpty();

        httpRequest.Headers.Add("Header1", "Value.1");
        httpRequest.Headers.Add("Header2", "Value.2");
        httpResponse.Headers.Add("Header3", "Value.3");
        httpResponse.Headers.Add("Header4", "Value.4");
        httpRequest.Headers.Add("Header5", string.Empty);
        httpResponse.Headers.Add("Header6", string.Empty);
        httpResponse.Headers.Add("Header7", "Value.7");

        var requestBuffer = new List<KeyValuePair<string, string>>();
        var responseBuffer = new List<KeyValuePair<string, string>>();
        var expectedRequest = new[]
        {
            new KeyValuePair<string, string>("Header1", Redacted),
            new KeyValuePair<string, string>("Header2", Redacted)
        };
        var expectedResponse = new[]
        {
            new KeyValuePair<string, string>("Header3", "Value.3"),
            new KeyValuePair<string, string>("Header4", "Value.4"),
            new KeyValuePair<string, string>("hEaDeR7", Redacted),
        };

        headersReader.ReadRequestHeaders(httpRequest, requestBuffer);
        headersReader.ReadResponseHeaders(httpResponse, responseBuffer);

        requestBuffer.Should().BeEquivalentTo(expectedRequest);
        responseBuffer.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public void HttpHeadersReader_WhenBufferIsNull_DoesNothing()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new LoggingOptions());
        var headersReader = new HttpHeadersReader(options, Mock.Of<IHttpHeadersRedactor>());
        List<KeyValuePair<string, string>>? responseBuffer = null;

        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();

        headersReader.ReadResponseHeaders(httpResponse, responseBuffer);
        responseBuffer.Should().BeNull();

        headersReader.ReadRequestHeaders(httpRequest, responseBuffer);
        responseBuffer.Should().BeNull();
    }
}

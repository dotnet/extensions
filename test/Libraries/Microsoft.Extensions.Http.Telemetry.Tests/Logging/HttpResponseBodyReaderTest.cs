// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;
using Microsoft.Shared.Diagnostics;
using Moq;
using Xunit;

using IOptionsFactory = Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpResponseBodyReaderTest
{
    private readonly Fixture _fixture;

    public HttpResponseBodyReaderTest()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Reader_NullOptions_Throws()
    {
        var options = IOptionsFactory.Create((LoggingOptions)null!);
        var act = () => new HttpResponseBodyReader(options);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Reader_SimpleContent_ReadsContent()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var expectedContentBody = _fixture.Create<string>();
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StringContent(expectedContentBody, Encoding.UTF8, "text/plain")
        };

        var responseBody = await httpResponseBodyReader.ReadAsync(httpResponse, CancellationToken.None).ConfigureAwait(false);

        responseBody.Should().BeEquivalentTo(expectedContentBody);
    }

    [Fact]
    public async Task Reader_EmptyContent_ErrorMessage()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StreamContent(new MemoryStream())
        };

        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var responseBody = await httpResponseBodyReader.ReadAsync(httpResponse, CancellationToken.None).ConfigureAwait(false);

        responseBody.Should().Be(Constants.NoContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task Reader_UnreadableContent_ErrorMessage(
        [CombinatorialValues("application/octet-stream", "image/png", "audio/ogg", "application/x-www-form-urlencoded",
            "application/javascript")]
        string contentType)
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var expectedContentBody = _fixture.Create<string>();
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StringContent(expectedContentBody, Encoding.UTF8, contentType)
        };

        var responseBody = await httpResponseBodyReader.ReadAsync(httpResponse, CancellationToken.None).ConfigureAwait(false);

        responseBody.Should().Be(Constants.UnreadableContent);
    }

    [Fact]
    public async Task Reader_OperationCanceled_ThrowsTaskCanceledException()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var input = _fixture.Create<string>();
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StringContent(input, Encoding.UTF8, "text/plain")
        };

        var token = new CancellationToken(true);

        var act = async () => await httpResponseBodyReader.ReadAsync(httpResponse, token);

        await act.Should().ThrowAsync<TaskCanceledException>().Where(e => e.CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [CombinatorialData]
    public async Task Reader_BigContent_TrimsAtTheEnd([CombinatorialValues(32, 256, 4095, 4096, 4097, 65536, 131072)] int limit)
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            BodySizeLimit = limit,
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var bigContent = RandomStringGenerator.Generate(limit * 2);
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StringContent(bigContent, Encoding.UTF8, "text/plain")
        };

        var responseBody = await httpResponseBodyReader.ReadAsync(httpResponse, CancellationToken.None).ConfigureAwait(false);

        responseBody.Should().Be(bigContent.Substring(0, limit));
    }

    [Fact]
    public async Task Reader_ReadingTakesTooLong_TimesOut()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            ResponseBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var httpResponseBodyReader = new HttpResponseBodyReader(options);
        var streamMock = new Mock<Stream>();
#if NETCOREAPP3_1_OR_GREATER
        streamMock.Setup(x => x.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
#else
        streamMock.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
#endif
        using var httpResponse = new HttpResponseMessage
        {
            Content = new StreamContent(streamMock.Object)
        };
        httpResponse.Content.Headers.Add("Content-type", "text/plain");

        var requestBody = await httpResponseBodyReader.ReadAsync(httpResponse, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().Be(Constants.ReadCancelled);
    }

    [Fact]
    public void HttpResponseBodyReader_Has_Infinite_Timeout_For_Reading_A_Body_When_Debugger_Is_Attached()
    {
        var options = IOptionsFactory.Create(new LoggingOptions());
        var reader = new HttpResponseBodyReader(options, DebuggerState.Attached);

        Assert.Equal(reader.ResponseReadTimeout, Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void HttpResponseBodyReader_Has_Option_Defined_Timeout_For_Reading_A_Body_When_Debugger_Is_Detached()
    {
        var timeout = TimeSpan.FromSeconds(274);
        var options = IOptionsFactory.Create(new LoggingOptions { BodyReadTimeout = timeout });
        var reader = new HttpResponseBodyReader(options, DebuggerState.Detached);

        Assert.Equal(reader.ResponseReadTimeout, timeout);
    }
}

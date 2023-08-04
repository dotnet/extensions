// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if false
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

public class HttpRequestBodyReaderTest
{
    private readonly Fixture _fixture;

    public HttpRequestBodyReaderTest()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Reader_NullOptions_Throws()
    {
        var options = IOptionsFactory.Create((LoggingOptions)null!);
        var act = () => new HttpRequestBodyReader(options);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Reader_SimpleContent_ReadsContent()
    {
        var input = _fixture.Create<string>();
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent(input, Encoding.UTF8, "text/plain"),
            Method = HttpMethod.Post
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);
        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().BeEquivalentTo(input);
    }

    [Fact]
    public async Task Reader_EmptyContent_ErrorMessage()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = new StreamContent(new MemoryStream()),
            Method = HttpMethod.Post
        };
        var httpRequestBodyReader = new HttpRequestBodyReader(options);

        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().BeEquivalentTo(Constants.NoContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task Reader_UnreadableContent_ErrorMessage(
                   [CombinatorialValues("application/octet-stream", "image/png", "audio/ogg", "application/x-www-form-urlencoded", "application/javascript")]
                       string contentType)
    {
        var input = _fixture.Create<string>();
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });

        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent(input, Encoding.UTF8, contentType),
            Method = HttpMethod.Post
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);
        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().Be(Constants.UnreadableContent);
    }

    [Fact]
    public async Task Reader_OperationCanceled_ThrowsTaskCanceledException()
    {
        var input = _fixture.Create<string>();
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent(input, Encoding.UTF8, "text/plain"),
            Method = HttpMethod.Post
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);
        var token = new CancellationToken(true);

        var act = async () =>
            await httpRequestBodyReader.ReadAsync(httpRequest, token).ConfigureAwait(false);

        await act.Should().ThrowAsync<TaskCanceledException>()
            .Where(e => e.CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [CombinatorialData]
    public async Task Reader_BigContent_TrimsAtTheEnd([CombinatorialValues(32, 256, 4095, 4096, 4097, 65536, 131072)] int limit)
    {
        var input = RandomStringGenerator.Generate(limit * 2);
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            BodySizeLimit = limit,
            RequestBodyContentTypes = new HashSet<string> { "text/plain" },
            BodyReadTimeout = TimeSpan.FromMinutes(100)
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent(input, Encoding.UTF8, "text/plain"),
            Method = HttpMethod.Post
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);
        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().BeEquivalentTo(input.Substring(0, limit));
    }

    [Theory]
    [CombinatorialData]
    public async Task Reader_SmallContentBigLimit_ReadsCorrectly([CombinatorialValues(32, 256, 4095, 4096, 4097, 65536, 131072)] int limit)
    {
        var input = RandomStringGenerator.Generate(limit / 2);
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            BodySizeLimit = limit,
            RequestBodyContentTypes = new HashSet<string> { "text/plain" },
            BodyReadTimeout = TimeSpan.FromMinutes(100)
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent(input, Encoding.UTF8, "text/plain"),
            Method = HttpMethod.Post
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);
        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().BeEquivalentTo(input.Substring(0, limit / 2));
    }

    [Fact]
    public async Task Reader_ReadingTakesTooLong_Timesout()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        var streamMock = new Mock<Stream>();
#if NETCOREAPP3_1_OR_GREATER
        streamMock.Setup(x => x.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
#else
        streamMock.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
#endif

        using var httpRequest = new HttpRequestMessage
        {
            Content = new StreamContent(streamMock.Object),
            Method = HttpMethod.Post
        };

        httpRequest.Content.Headers.Add("Content-type", "text/plain");

        var httpRequestBodyReader = new HttpRequestBodyReader(options);

        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        var returnedValue = requestBody;
        var expectedValue = Constants.ReadCancelled;

        returnedValue.Should().BeEquivalentTo(expectedValue);
    }

    [Fact]
    public async Task Reader_NullContent_ReturnsEmpty()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });
        using var httpRequest = new HttpRequestMessage
        {
            Content = null,
            Method = HttpMethod.Post
        };
        var httpRequestBodyReader = new HttpRequestBodyReader(options);

        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Reader_MethodIsGet_ReturnsEmpty()
    {
        var options = IOptionsFactory.Create(new LoggingOptions
        {
            RequestBodyContentTypes = new HashSet<string> { "text/plain" }
        });

        using var httpRequest = new HttpRequestMessage
        {
            Content = new StringContent("content", Encoding.UTF8, "text/plain"),
            Method = HttpMethod.Get
        };

        var httpRequestBodyReader = new HttpRequestBodyReader(options);

        var requestBody = await httpRequestBodyReader.ReadAsync(httpRequest, CancellationToken.None).ConfigureAwait(false);

        requestBody.Should().Be(string.Empty);
    }

    [Fact]
    public void HttpRequestBodyReader_Has_Infinite_Timeout_For_Reading_A_Body_When_Debugger_Is_Attached()
    {
        var options = IOptionsFactory.Create(new LoggingOptions());
        var reader = new HttpRequestBodyReader(options, DebuggerState.Attached);

        Assert.Equal(reader.RequestReadTimeout, Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void HttpRequestBodyReader_Has_Option_Defined_Timeout_For_Reading_A_Body_When_Debugger_Is_Detached()
    {
        var timeout = TimeSpan.FromSeconds(274);
        var options = IOptionsFactory.Create(new LoggingOptions { BodyReadTimeout = timeout });
        var reader = new HttpRequestBodyReader(options, DebuggerState.Detached);

        Assert.Equal(reader.RequestReadTimeout, timeout);
    }
}
#endif

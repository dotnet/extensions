// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Pools;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class ResponseInterceptingStreamTest
{
    [Fact]
    public async Task ResponseInterceptingStream_Calls_Intercepted_Stream_Methods()
    {
        var streamMock = new Mock<Stream>();
        const int Position = 1;
        const int WriteTimeout = 1;

        using var stream = new MemoryStream();
        using var responseInterceptingStream = new ResponseInterceptingStream(
                interceptedStream: streamMock.Object,
                responseBodyFeature: new StreamResponseBodyFeature(new MemoryStream()),
                bufferWriter: new BufferWriter<byte>(),
                interceptedValueWriteLimit: 1000);

        var writeResult = responseInterceptingStream.BeginWrite(Array.Empty<byte>(), 0, 0, _ => { }, new object());
        responseInterceptingStream.EndWrite(writeResult);
        var readResult = responseInterceptingStream.BeginRead(Array.Empty<byte>(), 0, 0, _ => { }, new object());
        responseInterceptingStream.EndRead(readResult);
        _ = responseInterceptingStream.CanRead;
        _ = responseInterceptingStream.CanSeek;
        _ = responseInterceptingStream.CanWrite;
        await responseInterceptingStream.CompleteAsync();
        responseInterceptingStream.CopyTo(stream, 10);
        await responseInterceptingStream.CopyToAsync(stream, 10, default);
        responseInterceptingStream.Flush();
        await responseInterceptingStream.FlushAsync(default);
        _ = responseInterceptingStream.Length;
        responseInterceptingStream.Position = Position;
        _ = responseInterceptingStream.Position;
        await responseInterceptingStream.ReadAsync(Array.Empty<byte>());
        _ = responseInterceptingStream.Seek(0, SeekOrigin.Current);
        responseInterceptingStream.SetLength(0);
        await responseInterceptingStream.WriteAsync(Array.Empty<byte>());
        _ = responseInterceptingStream.WriteTimeout;
        responseInterceptingStream.WriteTimeout = WriteTimeout;
        await responseInterceptingStream.ReadAsync(Array.Empty<byte>(), 0, 0, default);
        await responseInterceptingStream.WriteAsync(Array.Empty<byte>(), 0, 0, default);
        responseInterceptingStream.Read(Array.Empty<byte>(), 0, 0);
        await responseInterceptingStream.DisposeAsync();

        streamMock.Verify(
            expression: mock => mock.BeginWrite(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AsyncCallback>(), It.IsAny<object?>()),
            times: Times.AtLeastOnce);

        streamMock.Verify(
            expression: mock => mock.EndWrite(It.IsAny<IAsyncResult>()),
            times: Times.AtLeastOnce);

        streamMock.Verify(
            expression: mock => mock.BeginRead(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AsyncCallback>(), It.IsAny<object?>()),
            times: Times.AtLeastOnce);

        streamMock.Verify(
            expression: mock => mock.EndRead(It.IsAny<IAsyncResult>()),
            times: Times.AtLeastOnce);

        streamMock.Verify(
            expression: mock => mock.CanRead,
            times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.CanSeek,
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.CanWrite,
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.CopyTo(It.IsAny<Stream>(), It.IsAny<int>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.DisposeAsync(),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.Flush(),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.FlushAsync(It.IsAny<CancellationToken>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.Length,
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.Position,
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.SetLength(It.IsAny<long>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
             times: Times.AtLeast(2));

        streamMock.Verify(
             expression: mock => mock.WriteTimeout,
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()),
             times: Times.AtLeastOnce);

        streamMock.Verify(
             expression: mock => mock.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
             times: Times.AtLeastOnce);

        streamMock.VerifySet(x => x.Position = Position, Times.AtLeastOnce);
        streamMock.VerifySet(x => x.WriteTimeout = WriteTimeout, Times.AtLeastOnce);

        Assert.Equal(responseInterceptingStream, responseInterceptingStream.Stream);
    }

    [Fact]
    public async Task ResponseInterceptingStream_Calls_Underlying_HttpResponseBodyFeature_Methods()
    {
        var featureMock = new Mock<IHttpResponseBodyFeature>();

        using var responseInterceptingStream = new ResponseInterceptingStream(
                interceptedStream: new MemoryStream(),
                responseBodyFeature: featureMock.Object,
                bufferWriter: new BufferWriter<byte>(),
                interceptedValueWriteLimit: 1000);

        responseInterceptingStream.DisableBuffering();
        await responseInterceptingStream.StartAsync(default);
        await responseInterceptingStream.SendFileAsync(string.Empty, 0, 0, default);
        await responseInterceptingStream.CompleteAsync();

        featureMock.Verify(
            expression: mock => mock.DisableBuffering(),
            times: Times.AtLeastOnce);

        featureMock.Verify(
            expression: mock => mock.StartAsync(It.IsAny<CancellationToken>()),
            times: Times.AtLeastOnce);

        featureMock.Verify(
            expression: mock => mock.SendFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            times: Times.AtLeastOnce);

        featureMock.Verify(
            expression: mock => mock.CompleteAsync(),
            times: Times.AtLeastOnce);
    }

    [Fact]
    public void Consumer_Can_Write_Or_Read_From_InterceptingStream_Using_Span_Overloads()
    {
        using var responseInterceptingStream = new ResponseInterceptingStream(
                interceptedStream: new MemoryStream(),
                responseBodyFeature: new StreamResponseBodyFeature(new MemoryStream()),
                bufferWriter: new BufferWriter<byte>(),
                interceptedValueWriteLimit: 1000);

        var data = Encoding.UTF8.GetBytes("Kebab");

        responseInterceptingStream.Write(data, 0, data.Length);

        Assert.Equal(responseInterceptingStream.Position, data.Length);

        responseInterceptingStream.Seek(0, SeekOrigin.Begin);

        var buffer = new byte[data.Length];
        responseInterceptingStream.Read(buffer);

        Assert.Equal(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(buffer));
    }
}

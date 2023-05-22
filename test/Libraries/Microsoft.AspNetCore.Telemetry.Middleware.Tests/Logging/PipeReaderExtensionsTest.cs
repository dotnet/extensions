// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class PipeReaderExtensionsTest
{
    [Theory]
    [InlineData("T", 32)]
    [InlineData("Hr", 2)]
    [InlineData("AG8", 2)]
    [InlineData("IpR7E", 5)]
    [InlineData("DSbwoedf", 16)]
    [InlineData("dthT18LsIaZNy", 1)]
    [InlineData("ShOXqmhQyLFxW78V4DgwE", 11)]
    [InlineData("FB3GefYUQxQeDiKnqtXglzd2szS2o7X6ei", 64)]
    [InlineData("qREdmuDaoNmWdC2gbhD3rsRVke6FloRlw7fbM0of7d6RTEXGGc5D3HF", 64)]
    public async Task ReadAsync_VariousSizesDefaultPipeReader(string content, int numBytes)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var pipe = PipeReader.Create(stream);

        var result = await pipe.ReadAsync(numBytes, CancellationToken.None);

        Assert.Equal(
            content.Substring(0, Math.Min(content.Length, numBytes)),
            Encoding.UTF8.GetString(result.ToArray()));
    }

    [Theory]
    [InlineData("T", 32)]
    [InlineData("Hr", 2)]
    [InlineData("AG8", 2)]
    [InlineData("IpR7E", 5)]
    [InlineData("DSbwoedf", 16)]
    [InlineData("dthT18LsIaZNy", 1)]
    [InlineData("ShOXqmhQyLFxW78V4DgwE", 11)]
    [InlineData("FB3GefYUQxQeDiKnqtXglzd2szS2o7X6ei", 64)]
    [InlineData("qREdmuDaoNmWdC2gbhD3rsRVke6FloRlw7fbM0of7d6RTEXGGc5D3HF", 64)]
    public async Task ReadAsync_VariousSizesSmallBuffersPipeReader(string content, int numBytes)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var options = new StreamPipeReaderOptions(bufferSize: 16, minimumReadSize: 16);
        var pipe = PipeReader.Create(stream, options);

        var result = await pipe.ReadAsync(numBytes, CancellationToken.None);

        Assert.Equal(
            content.Substring(0, Math.Min(content.Length, numBytes)),
            Encoding.UTF8.GetString(result.ToArray()));
    }
}

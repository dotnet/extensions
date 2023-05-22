// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class HttpRequestBodyReaderTest
{
    [Fact]
    public async Task Should_ThrowOnCancellation()
    {
        var context = new DefaultHttpContext();
        using var stream = CreateMemoryStream("test");
        context.Request.Body = stream;
        context.Request.ContentType = "text/plain";

        var ex = await Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
                await HttpRequestBodyReader.ReadBodyAsync(context.Request, TimeSpan.FromMinutes(100), int.MaxValue, new(canceled: true)));

        Assert.True(ex.CancellationToken.IsCancellationRequested);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task FormatAsync_WhenTimeoutReading_ErrorMessage()
    {
        var context = new DefaultHttpContext();
        using var stream = new InfiniteStream('A');
        context.Request.Body = stream;
        context.Request.ContentType = "text/plain";

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(100));

        var body = await HttpRequestBodyReader.ReadBodyAsync(context.Request, TimeSpan.FromMilliseconds(100), int.MaxValue, cts.Token);

        Assert.Equal(HttpRequestBodyReader.ReadCancelled, Encoding.UTF8.GetString(body.FirstSpan));
        Assert.Equal(0, stream.Position);
    }

    private static MemoryStream CreateMemoryStream(string value)
        => new(Encoding.UTF8.GetBytes(value));
}

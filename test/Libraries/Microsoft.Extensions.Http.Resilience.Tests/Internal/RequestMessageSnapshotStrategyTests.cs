// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internal;

public class RequestMessageSnapshotStrategyTests
{
    [Fact]
    public async Task SendAsync_EnsureSnapshotAttached()
    {
        var strategy = Create();
        var context = ResilienceContextPool.Shared.Get();
        using var request = new HttpRequestMessage();
        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        using var response = await strategy.ExecuteAsync(
            context =>
            {
                context.Properties.GetValue(ResilienceKeys.RequestSnapshot, null!).Should().NotBeNull();
                return new ValueTask<HttpResponseMessage>(new HttpResponseMessage());
            },
            context);
    }

    [Fact]
    public void ExecuteAsync_RequestMessageNotFound_Throws()
    {
        var strategy = Create();

        strategy.Invoking(s => s.Execute(() => { })).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteCoreAsync_IOExceptionThrownWhenCreatingSnapshot_ReturnsExceptionOutcome()
    {
        var strategy = Create();
        var context = ResilienceContextPool.Shared.Get();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://www.example.com/some-resource"));
        using var stream = new StreamTestHelper("some stream content");
        request.Content = new StreamContent(stream);
        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        _ = await Assert.ThrowsAsync<IOException>(async () => await strategy.ExecuteAsync(context => default, context));
    }

    private static ResiliencePipeline Create() => new ResiliencePipelineBuilder().AddStrategy(_ => new RequestMessageSnapshotStrategy(), Mock.Of<ResilienceStrategyOptions>()).Build();

    private class StreamTestHelper : MemoryStream
    {
        public StreamTestHelper(string str)
            : base(Encoding.UTF8.GetBytes(str))
        {
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new IOException();

#if NET5_0_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException();
#else
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException();
#endif
    }
}

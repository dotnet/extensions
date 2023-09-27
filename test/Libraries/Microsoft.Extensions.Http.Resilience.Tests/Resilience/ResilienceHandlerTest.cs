// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;

public class ResilienceHandlerTest
{
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureRequestMetadataFlows(bool resilienceContextSet)
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        request.SetRequestMetadata(new RequestMetadata());

        if (resilienceContextSet)
        {
            request.SetResilienceContext(ResilienceContextPool.Shared.Get());
        }

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await invoker.SendAsync(request, default);

        if (resilienceContextSet)
        {
            request.GetResilienceContext()!
                .Properties
                .GetValue(new ResiliencePropertyKey<RequestMetadata>(TelemetryConstants.RequestMetadataKey), null!)
                .Should()
                .NotBeNull();
        }
        else
        {
            request.GetResilienceContext().Should().BeNull();
        }
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureExecutionContext(bool executionContextSet)
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetResilienceContext(ResilienceContextPool.Shared.Get());
        }

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await invoker.SendAsync(request, default);

        if (executionContextSet)
        {
            Assert.NotNull(request.GetResilienceContext());
        }
        else
        {
            Assert.Null(request.GetResilienceContext());
        }
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureInvoker(bool executionContextSet)
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetResilienceContext(ResilienceContextPool.Shared.Get());
        }

        handler.InnerHandler = new TestHandlerStub((r, _) =>
        {
            r.GetResilienceContext().Should().NotBeNull();
            r.GetResilienceContext()!.Properties.GetValue(ResilienceKeys.RequestMessage, null!).Should().BeSameAs(r);

            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });
        });

        var response = await invoker.SendAsync(request, default);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_EnsureCancellationTokenFlowsToResilienceContext()
    {
        using var source = new CancellationTokenSource();
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        handler.InnerHandler = new TestHandlerStub((_, cancellationToken) =>
        {
            cancellationToken.Should().Be(source.Token);

            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });
        });

        var response = await invoker.SendAsync(request, source.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_Exception_EnsureRethrown()
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        handler.InnerHandler = new TestHandlerStub((_, _) => throw new InvalidOperationException());

        await invoker.Invoking(i => i.SendAsync(request, default)).Should().ThrowAsync<InvalidOperationException>();
    }
}

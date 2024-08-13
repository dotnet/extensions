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
    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
    [InlineData(false)]
#endif
    public async Task Send_EnsureRequestMetadataFlows(bool resilienceContextSet, bool asynchronous = true)
    {
        using var handler = new ResilienceHandler(ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        request.SetRequestMetadata(new RequestMetadata());

        if (resilienceContextSet)
        {
            request.SetResilienceContext(ResilienceContextPool.Shared.Get());
        }

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await InvokeHandler(invoker, request, asynchronous);

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

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
    [InlineData(false)]
#endif
    public async Task Send_EnsureExecutionContext(bool executionContextSet, bool asynchronous = true)
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetResilienceContext(ResilienceContextPool.Shared.Get());
        }

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await InvokeHandler(invoker, request, asynchronous);

        if (executionContextSet)
        {
            Assert.NotNull(request.GetResilienceContext());
        }
        else
        {
            Assert.Null(request.GetResilienceContext());
        }
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
    [InlineData(false)]
#endif
    public async Task Send_EnsureInvoker(bool executionContextSet, bool asynchronous = true)
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

        var response = await InvokeHandler(invoker, request, asynchronous);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task Send_EnsureCancellationTokenFlowsToResilienceContext(bool asynchronous = true)
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

        var response = await InvokeHandler(invoker, request, asynchronous, source.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task Send_Exception_EnsureRethrown(bool asynchronous = true)
    {
        using var handler = new ResilienceHandler(_ => ResiliencePipeline<HttpResponseMessage>.Empty);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        handler.InnerHandler = new TestHandlerStub((_, _) => throw new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeHandler(invoker, request, asynchronous));
    }

    private static Task<HttpResponseMessage> InvokeHandler(
        HttpMessageInvoker invoker,
        HttpRequestMessage request,
        bool asynchronous,
        CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        if (asynchronous)
        {
            return invoker.SendAsync(request, cancellationToken);
        }
        else
        {
            return Task.FromResult(invoker.Send(request, cancellationToken));
        }
#else
        return invoker.SendAsync(request, cancellationToken);
#endif
    }
}

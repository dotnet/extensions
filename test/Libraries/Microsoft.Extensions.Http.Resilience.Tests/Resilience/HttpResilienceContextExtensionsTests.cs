// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class HttpResilienceContextExtensionsTests
{
    [Fact]
    public void GetRequestMessage_ResilienceContextIsNull_Throws()
    {
        ResilienceContext context = null!;
        Assert.Throws<ArgumentNullException>(context.GetRequestMessage);
    }

    [Fact]
    public void GetRequestMessage_RequestMessageIsMissing_ReturnsNull()
    {
        var context = ResilienceContextPool.Shared.Get();

        Assert.Null(context.GetRequestMessage());

        ResilienceContextPool.Shared.Return(context);
    }

    [Fact]
    public void GetRequestMessage_RequestMessageIsNull_ReturnsNull()
    {
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(ResilienceKeys.RequestMessage, null);

        Assert.Null(context.GetRequestMessage());

        ResilienceContextPool.Shared.Return(context);
    }

    [Fact]
    public void GetRequestMessage_RequestMessageIsPresent_ReturnsRequestMessage()
    {
        var context = ResilienceContextPool.Shared.Get();
        using var request = new HttpRequestMessage();
        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        Assert.Same(request, context.GetRequestMessage());

        ResilienceContextPool.Shared.Return(context);
    }

    [Fact]
    public void SetRequestMessage_ResilienceContextIsNull_Throws()
    {
        ResilienceContext context = null!;
        using var request = new HttpRequestMessage();
        Assert.Throws<ArgumentNullException>(() => context.SetRequestMessage(request));
    }

    [Fact]
    public void SetRequestMessage_RequestMessageIsNull_SetsNullRequestMessage()
    {
        var context = ResilienceContextPool.Shared.Get();
        context.SetRequestMessage(null);

        Assert.True(context.Properties.TryGetValue(ResilienceKeys.RequestMessage, out HttpRequestMessage? request));
        Assert.Null(request);

        ResilienceContextPool.Shared.Return(context);
    }

    [Fact]
    public void SetRequestMessage_RequestMessageIsNotNull_SetsRequestMessage()
    {
        var context = ResilienceContextPool.Shared.Get();
        using var request = new HttpRequestMessage();
        context.SetRequestMessage(request);

        Assert.True(context.Properties.TryGetValue(ResilienceKeys.RequestMessage, out HttpRequestMessage? actualRequest));
        Assert.Same(request, actualRequest);

        ResilienceContextPool.Shared.Return(context);
    }
}

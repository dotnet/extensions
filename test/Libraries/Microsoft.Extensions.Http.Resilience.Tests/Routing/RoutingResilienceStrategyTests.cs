// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public class RoutingResilienceStrategyTests
{
    [Fact]
    public void NoRequestMessage_Throws()
    {
        var strategy = Create(() => Mock.Of<RequestRoutingStrategy>());

        strategy.Invoking(s => s.Execute(() => { })).Should().Throw<InvalidOperationException>().WithMessage("The HTTP request message was not found in the resilience context.");
    }

    [Fact]
    public void RequestMessageIsNull_Throws()
    {
        var strategy = Create(() => Mock.Of<RequestRoutingStrategy>());
        var context = ResilienceContextPool.Shared.Get();
        context.SetRequestMessage(null);

        strategy.Invoking(s => s.Execute(_ => { }, context)).Should().Throw<InvalidOperationException>().WithMessage("The HTTP request message was not found in the resilience context.");
    }

    [Fact]
    public void NoRoutingProvider_Ok()
    {
        using var request = new HttpRequestMessage();

        var strategy = Create(null);
        var context = ResilienceContextPool.Shared.Get();
        context.SetRequestMessage(request);

        strategy.Invoking(s => s.Execute(_ => { }, context)).Should().NotThrow();
    }

    private static ResiliencePipeline Create(Func<RequestRoutingStrategy>? provider) =>
        new ResiliencePipelineBuilder().AddStrategy(_ => new RoutingResilienceStrategy(provider), Mock.Of<ResilienceStrategyOptions>()).Build();

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public class RoutingResilienceStrategyTests
{
    [Fact]
    public void NoRequestMessage_Throws()
    {
        RoutingResilienceStrategy strategy = new RoutingResilienceStrategy(() => Mock.Of<RequestRoutingStrategy>());

        strategy.Invoking(s => s.Execute(() => { })).Should().Throw<InvalidOperationException>().WithMessage("The HTTP request message was not found in the resilience context.");
    }
}

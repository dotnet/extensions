// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

public class RoutingHelperTest
{
    [InlineData(1.5d, 2)]
    [InlineData(0d, 1)]
    [Theory]
    public void SelectEndpoint_Ok(double nextResult, int expectedEndpoint)
    {
        var randomizer = new Mock<IRandomizer>(MockBehavior.Strict);
        randomizer.Setup(v => v.NextDouble(10)).Returns(nextResult);

        var result = RoutingHelper.SelectByWeight(new List<int> { 1, 2, 3, 4 }, v => v, randomizer.Object);

        Assert.Equal(expectedEndpoint, result);
    }

    [Fact]
    public void SelectEndpoint_Invalid()
    {
        var randomizer = new Mock<IRandomizer>(MockBehavior.Strict);
        randomizer.Setup(v => v.NextDouble(10)).Returns(10000);

        Assert.Throws<InvalidOperationException>(() => RoutingHelper.SelectByWeight(new List<int> { 1, 2, 3, 4 }, v => v, randomizer.Object));
    }
}

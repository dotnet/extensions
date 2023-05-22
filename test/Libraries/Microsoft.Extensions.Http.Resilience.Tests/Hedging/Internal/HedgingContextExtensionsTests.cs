// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

public class HedgingContextExtensionsTests
{
    [Fact]
    public void GetSet_RoutingStrategy_Ok()
    {
        var setter = HedgingContextExtensions.CreateRoutingStrategySetter("my-pipeline");
        var getter = HedgingContextExtensions.CreateRoutingStrategyProvider("my-pipeline");
        var getterInvalid = HedgingContextExtensions.CreateRoutingStrategyProvider("my-other-pipeline");

        var context = new Context();
        var strategy = Mock.Of<IRequestRoutingStrategy>();

        setter(context, strategy);

        Assert.Equal(strategy, getter(context));
        Assert.Null(getterInvalid(context));
    }

    [Fact]
    public void GetSet_HttpRequestMessageSnapshot_Ok()
    {
        var setter = HedgingContextExtensions.CreateRequestMessageSnapshotSetter("my-pipeline");
        var getter = HedgingContextExtensions.CreateRequestMessageSnapshotProvider("my-pipeline");
        var getterInvalid = HedgingContextExtensions.CreateRequestMessageSnapshotProvider("my-other-pipeline");

        var context = new Context();
        var snapshot = Mock.Of<IHttpRequestMessageSnapshot>();

        setter(context, snapshot);

        Assert.Equal(snapshot, getter(context));
        Assert.Null(getterInvalid(context));
    }
}

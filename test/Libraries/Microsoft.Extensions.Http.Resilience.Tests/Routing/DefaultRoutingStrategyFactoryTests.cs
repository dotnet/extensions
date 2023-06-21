// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public sealed class DefaultRoutingStrategyFactoryTests : IDisposable
{
    private const string ClientName = "clientName";
    private readonly Uri _routeUri = new("https://bing.com");
    private readonly Mock<IStubRoutingService> _mockService = new();
    public void Dispose()
    {
        _mockService.VerifyAll();
    }

    [Fact]
    public void CreateRoutingStrategy_ShouldCreateRoutingStrategyAndPassTheName()
    {
        var serviceCollection = new ServiceCollection();
        _mockService.Setup(s => s.Route).Returns(_routeUri);

        serviceCollection.AddSingleton(_mockService.Object);
        using var provider = serviceCollection.BuildServiceProvider();
        var factory = new DefaultRoutingStrategyFactory<MockRoutingStrategy>(ClientName, provider);

        var routingStrategy = factory.CreateRoutingStrategy();
        Uri? resultRouteUri = null;
        routingStrategy?.TryGetNextRoute(out resultRouteUri);

        var mockRoutingStrategy = routingStrategy as MockRoutingStrategy;

        Assert.NotNull(mockRoutingStrategy);

        Assert.Equal(ClientName, mockRoutingStrategy?.Name);
        Assert.Equal(_routeUri, resultRouteUri!);
    }

    [Fact]
    public void CreateRoutingStrategy_WhenServiceIsNotInjectedShouldThrow()
    {
        var serviceCollection = new ServiceCollection();
        using var provider = serviceCollection.BuildServiceProvider();

        var factory = new DefaultRoutingStrategyFactory<MockRoutingStrategy>(ClientName, provider);

        Assert.Throws<InvalidOperationException>(() =>
        {
            factory.CreateRoutingStrategy();
        });
    }
}

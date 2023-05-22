// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

// Can't use NotNullWhenAttribute since it's defined in two reference assemblies with InternalVisibleTo
#pragma warning disable CS8767

internal class MockRoutingStrategy : IRequestRoutingStrategy
{
    private readonly IStubRoutingService _mockService;

    public MockRoutingStrategy(IStubRoutingService mockService, string name)
    {
        _mockService = mockService;
        Name = name;
    }

    public string Name { get; private set; }

    public bool TryGetNextRoute(out Uri? nextRoute)
    {
        nextRoute = _mockService.Route;
        return true;
    }
}

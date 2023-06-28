// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

// Can't use NotNullWhenAttribute since it's defined in two reference assemblies with InternalVisibleTo
#pragma warning disable CS8767

internal class MockRoutingStrategy : RequestRoutingStrategy
{
    private readonly IStubRoutingService _mockService;

    public MockRoutingStrategy(IStubRoutingService mockService, string name)
        : base(new Randomizer())
    {
        _mockService = mockService;
        Name = name;
    }

    public string Name { get; private set; }

    public override void Dispose()
    {
    }

    public override bool TryGetNextRoute(out Uri? nextRoute)
    {
        nextRoute = _mockService.Route;
        return true;
    }

    public override bool TryReset()
    {
        return true;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Internal.Routing;

internal sealed class OrderedGroupsRoutingStrategy : IRequestRoutingStrategy, IResettable
{
    private readonly IRandomizer _randomizer;

    private int _lastUsedIndex;
    private IList<EndpointGroup>? _groups;

    public OrderedGroupsRoutingStrategy(IRandomizer randomizer)
    {
        _randomizer = randomizer;
    }

    public void Initialize(IList<EndpointGroup> groups)
    {
        _ = TryReset();

        _groups = groups;
    }

    public bool TryGetNextRoute([NotNullWhen(true)] out Uri? nextRoute)
    {
        if (_groups == null)
        {
            Throw.InvalidOperationException("The routing strategy is not initialized.");
        }

        if (TryGetNextGroup(out var group))
        {
            nextRoute = group!.Endpoints.SelectByWeight(e => e.Weight, _randomizer!).Uri!;
            return true;
        }

        nextRoute = null;
        return false;
    }

    public bool TryReset()
    {
        _groups = null;
        _lastUsedIndex = 0;
        return true;
    }

    private bool TryGetNextGroup(out EndpointGroup? nextGroup)
    {
        if (_lastUsedIndex >= _groups!.Count)
        {
            nextGroup = null;
            return false;
        }

        nextGroup = _groups[_lastUsedIndex];
        _lastUsedIndex++;
        return true;
    }
}

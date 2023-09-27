// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal.OrderedGroups;

internal sealed class OrderedGroupsRoutingStrategy : RequestRoutingStrategy, IResettable
{
    private readonly ObjectPool<OrderedGroupsRoutingStrategy> _pool;
    private int _lastUsedIndex;
    private IList<UriEndpointGroup>? _groups;

    public OrderedGroupsRoutingStrategy(Randomizer randomizer, ObjectPool<OrderedGroupsRoutingStrategy> pool)
        : base(randomizer)
    {
        _pool = pool;
    }

    public void Initialize(IList<UriEndpointGroup> groups)
    {
        _ = TryReset();

        _groups = groups;
    }

    public override bool TryGetNextRoute([NotNullWhen(true)] out Uri? nextRoute)
    {
        if (_groups == null)
        {
            Throw.InvalidOperationException("The routing strategy is not initialized.");
        }

        if (TryGetNextGroup(out var group))
        {
            nextRoute = group!.Endpoints.SelectByWeight(e => e.Weight, Randomizer!).Uri!;
            return true;
        }

        nextRoute = null;
        return false;
    }

    public override void Dispose() => _pool.Return(this);

    public override bool TryReset()
    {
        _groups = null;
        _lastUsedIndex = 0;
        return true;
    }

    private bool TryGetNextGroup(out UriEndpointGroup? nextGroup)
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

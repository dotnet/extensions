// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal.WeightedGroups;

internal sealed class WeightedGroupsRoutingStrategy : RequestRoutingStrategy
{
    private readonly List<WeightedEndpointGroup> _groups;
    private readonly ObjectPool<WeightedGroupsRoutingStrategy> _pool;
    private bool _initialGroupPicked;
    private WeightedGroupSelectionMode _mode;
    private bool _initialized;

    public WeightedGroupsRoutingStrategy(Randomizer randomizer, ObjectPool<WeightedGroupsRoutingStrategy> pool)
        : base(randomizer)
    {
        _groups = new List<WeightedEndpointGroup>();
        _pool = pool;
    }

    public void Initialize(IEnumerable<WeightedEndpointGroup> groups, WeightedGroupSelectionMode mode)
    {
        _ = TryReset();

        _initialized = true;
        _mode = mode;
        _groups.AddRange(groups);
    }

    public override void Dispose() => _pool.Return(this);

    public override bool TryReset()
    {
        _initialized = false;
        _mode = WeightedGroupSelectionMode.EveryAttempt;
        _groups.Clear();
        _initialGroupPicked = false;
        return true;
    }

    public override bool TryGetNextRoute([NotNullWhen(true)] out Uri? nextRoute)
    {
        if (!_initialized)
        {
            Throw.InvalidOperationException("The routing strategy is not initialized.");
        }

        if (TryGetNextGroup(out var group))
        {
            nextRoute = group!.Endpoints.SelectByWeight(e => e.Weight, Randomizer).Uri!;
            return true;
        }

        nextRoute = null;
        return false;
    }

    private bool TryGetNextGroup(out WeightedEndpointGroup? nextGroup)
    {
        if (_groups.Count == 0)
        {
            nextGroup = null;
            return false;
        }

        nextGroup = PickGroup();
        _ = _groups.Remove(nextGroup);
        return true;
    }

    private WeightedEndpointGroup PickGroup()
    {
        if (!_initialGroupPicked)
        {
            _initialGroupPicked = true;
            return _groups.SelectByWeight(g => g.Weight, Randomizer);
        }

        if (_mode == WeightedGroupSelectionMode.InitialAttempt)
        {
            return _groups[0];
        }
        else
        {
            return _groups.SelectByWeight(g => g.Weight, Randomizer);
        }
    }
}

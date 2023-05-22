// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Internal.Routing;

internal sealed class WeightedGroupsRoutingStrategy : IRequestRoutingStrategy, IResettable
{
    private readonly IRandomizer _randomizer;
    private readonly List<WeightedEndpointGroup> _groups;
    private bool _initialGroupPicked;
    private WeightedGroupSelectionMode _mode;
    private bool _initialized;

    public WeightedGroupsRoutingStrategy(IRandomizer randomizer)
    {
        _randomizer = randomizer;
        _groups = new List<WeightedEndpointGroup>();
    }

    public void Initialize(IEnumerable<WeightedEndpointGroup> groups, WeightedGroupSelectionMode mode)
    {
        _ = TryReset();

        _initialized = true;
        _mode = mode;
        _groups.AddRange(groups);
    }

    public bool TryReset()
    {
        _initialized = false;
        _mode = WeightedGroupSelectionMode.EveryAttempt;
        _groups.Clear();
        _initialGroupPicked = false;
        return true;
    }

    public bool TryGetNextRoute([NotNullWhen(true)] out Uri? nextRoute)
    {
        if (!_initialized)
        {
            Throw.InvalidOperationException("The routing strategy is not initialized.");
        }

        if (TryGetNextGroup(out var group))
        {
            nextRoute = group!.Endpoints.SelectByWeight(e => e.Weight, _randomizer).Uri!;
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
            return _groups.SelectByWeight(g => g.Weight, _randomizer);
        }

        if (_mode == WeightedGroupSelectionMode.InitialAttempt)
        {
            return _groups[0];
        }
        else
        {
            return _groups.SelectByWeight(g => g.Weight, _randomizer);
        }
    }
}

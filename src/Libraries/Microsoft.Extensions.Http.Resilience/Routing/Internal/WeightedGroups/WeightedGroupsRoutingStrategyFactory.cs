// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal.WeightedGroups;

internal sealed class WeightedGroupsRoutingStrategyFactory : IPooledObjectPolicy<WeightedGroupsRoutingStrategy>
{
    private readonly Randomizer _randomizer;
    private readonly NamedOptionsCache<WeightedGroupsRoutingOptions> _cache;
    private readonly ObjectPool<WeightedGroupsRoutingStrategy> _pool;

    public WeightedGroupsRoutingStrategyFactory(Randomizer randomizer, NamedOptionsCache<WeightedGroupsRoutingOptions> cache)
    {
        _randomizer = randomizer;
        _cache = cache;
#pragma warning disable S3366 // "this" should not be exposed from constructors
        _pool = PoolFactory.CreatePool(this);
#pragma warning restore S3366 // "this" should not be exposed from constructors
    }

    public WeightedGroupsRoutingStrategy Get()
    {
        var strategy = _pool.Get();
        strategy.Initialize(_cache.Options.Groups, _cache.Options.SelectionMode);
        return strategy;
    }

    WeightedGroupsRoutingStrategy IPooledObjectPolicy<WeightedGroupsRoutingStrategy>.Create() => new(_randomizer, _pool);

    bool IPooledObjectPolicy<WeightedGroupsRoutingStrategy>.Return(WeightedGroupsRoutingStrategy obj) => obj.TryReset();
}

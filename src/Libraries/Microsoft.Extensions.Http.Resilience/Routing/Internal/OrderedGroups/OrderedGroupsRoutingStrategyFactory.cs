// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal.OrderedGroups;

internal sealed class OrderedGroupsRoutingStrategyFactory : IPooledObjectPolicy<OrderedGroupsRoutingStrategy>
{
    private readonly Randomizer _randomizer;
    private readonly NamedOptionsCache<OrderedGroupsRoutingOptions> _cache;
    private readonly ObjectPool<OrderedGroupsRoutingStrategy> _pool;

    public OrderedGroupsRoutingStrategyFactory(Randomizer randomizer, NamedOptionsCache<OrderedGroupsRoutingOptions> cache)
    {
        _randomizer = randomizer;
        _cache = cache;
#pragma warning disable S3366 // "this" should not be exposed from constructors
        _pool = PoolFactory.CreatePool(this);
#pragma warning restore S3366 // "this" should not be exposed from constructors
    }

    public OrderedGroupsRoutingStrategy Get()
    {
        var strategy = _pool.Get();
        strategy.Initialize(_cache.Options.Groups);
        return strategy;
    }

    public void Return(OrderedGroupsRoutingStrategy strategy) => _pool.Return(strategy);

    OrderedGroupsRoutingStrategy IPooledObjectPolicy<OrderedGroupsRoutingStrategy>.Create() => new(_randomizer, _pool);

    bool IPooledObjectPolicy<OrderedGroupsRoutingStrategy>.Return(OrderedGroupsRoutingStrategy obj) => obj.TryReset();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Routing;

internal sealed class OrderedGroupsRoutingStrategyFactory : PooledRoutingStrategyFactory<OrderedGroupsRoutingStrategy, OrderedGroupsRoutingOptions>
{
    public OrderedGroupsRoutingStrategyFactory(string clientId, ObjectPool<OrderedGroupsRoutingStrategy> pool, IOptionsMonitor<OrderedGroupsRoutingOptions> optionsMonitor)
        : base(clientId, pool, optionsMonitor)
    {
    }

    protected override void Initialize(OrderedGroupsRoutingStrategy strategy, OrderedGroupsRoutingOptions options) => strategy.Initialize(options.Groups);
}

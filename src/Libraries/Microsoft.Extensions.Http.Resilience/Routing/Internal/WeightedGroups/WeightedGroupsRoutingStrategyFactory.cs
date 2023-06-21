// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal.WeightedGroups;

internal sealed class WeightedGroupsRoutingStrategyFactory : PooledRoutingStrategyFactory<WeightedGroupsRoutingStrategy, WeightedGroupsRoutingOptions>
{
    public WeightedGroupsRoutingStrategyFactory(string clientId, ObjectPool<WeightedGroupsRoutingStrategy> pool, IOptionsMonitor<WeightedGroupsRoutingOptions> optionsMonitor)
        : base(clientId, pool, optionsMonitor)
    {
    }

    protected override void Initialize(WeightedGroupsRoutingStrategy strategy, WeightedGroupsRoutingOptions options) => strategy.Initialize(options.Groups, options.SelectionMode);
}

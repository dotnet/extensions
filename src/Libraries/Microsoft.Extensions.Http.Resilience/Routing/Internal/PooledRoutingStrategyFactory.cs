// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

internal abstract class PooledRoutingStrategyFactory<T, TOptions> : IPooledRequestRoutingStrategyFactory
    where T : class, IRequestRoutingStrategy, IResettable
{
    private readonly ObjectPool<T> _pool;
    private TOptions _options;

    protected PooledRoutingStrategyFactory(string clientId, ObjectPool<T> pool, IOptionsMonitor<TOptions> optionsMonitor)
    {
        _pool = pool;
        _options = optionsMonitor.Get(clientId);

        _ = optionsMonitor.OnChange((options, name) =>
        {
            if (name == clientId)
            {
                _options = options;
            }
        });
    }

    public IRequestRoutingStrategy CreateRoutingStrategy()
    {
        var strategy = _pool.Get();

        Initialize(strategy, _options);

        return strategy;
    }

    public void ReturnRoutingStrategy(IRequestRoutingStrategy strategy)
    {
        _ = Throw.IfNull(strategy);

        _pool.Return((T)strategy);
    }

    protected abstract void Initialize(T strategy, TOptions options);
}

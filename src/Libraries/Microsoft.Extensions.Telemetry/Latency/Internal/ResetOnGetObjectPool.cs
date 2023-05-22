// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Latency.Internal;

internal sealed class ResetOnGetObjectPool<T> : ObjectPool<T>
    where T : class, IResettable
{
    private readonly ObjectPool<T> _objectPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetOnGetObjectPool{T}"/> class.
    /// </summary>
    public ResetOnGetObjectPool(PooledObjectPolicy<T> policy)
    {
        _objectPool = PoolFactory.CreatePool(policy);
    }

    public override T Get()
    {
        var o = _objectPool.Get();
        _ = o.TryReset();
        return o;
    }

    public override void Return(T obj)
    {
        _objectPool.Return(obj);
    }
}

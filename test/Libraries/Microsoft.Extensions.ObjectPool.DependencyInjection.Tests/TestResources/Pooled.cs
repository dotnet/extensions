// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.ObjectPool.Test.TestResources;

public sealed class Pooled<TService> : IDisposable
    where TService : class
{
    private readonly ObjectPool<TService> _pool;

    public TService Object { get; }

    public Pooled(ObjectPool<TService> pool)
    {
        _pool = pool;
        Object = pool.Get();
    }

    public void Dispose()
    {
        _pool.Return(Object);
    }
}

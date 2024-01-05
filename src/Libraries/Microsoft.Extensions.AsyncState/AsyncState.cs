// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.AsyncState;

internal sealed class AsyncState : IAsyncState
{
    private static readonly AsyncLocal<AsyncStateHolder> _asyncContextCurrent = new();
    private static readonly ObjectPool<ConcurrentDictionary<AsyncStateToken, object?>> _featuresPool = PoolFactory.CreatePool(new ConcurrentDictionaryPooledPolicy());
    private int _contextCount;

    public void Initialize()
    {
        Reset();

        // Use an object indirection to hold the AsyncContext in the AsyncLocal,
        // so it can be cleared in all ExecutionContexts when its cleared.
        var features = new AsyncStateHolder
        {
            Dictionary = _featuresPool.Get()
        };

        _asyncContextCurrent.Value = features;
    }

    public void Reset()
    {
        var holder = _asyncContextCurrent.Value;
        if (holder != null)
        {
            // Clear current AsyncContext trapped in the AsyncLocals, as its done.
            if (holder.Dictionary != null)
            {
                _featuresPool.Return(holder.Dictionary);
                holder.Dictionary = null;
            }
        }
    }

    public AsyncStateToken RegisterAsyncContext()
    {
        return new AsyncStateToken(Interlocked.Increment(ref _contextCount) - 1);
    }

    public bool TryGet(AsyncStateToken token, out object? value)
    {
        // Context is not initialized
#pragma warning disable EA0011
        if (_asyncContextCurrent.Value?.Dictionary == null)
#pragma warning restore EA0011
        {
            value = null;
            return false;
        }

        _ = _asyncContextCurrent.Value.Dictionary.TryGetValue(token, out value);
        return true;
    }

    public object? Get(AsyncStateToken token)
    {
        if (TryGet(token, out object? value))
        {
            return value;
        }

        throw new InvalidOperationException("Context is not initialized");
    }

    public void Set(AsyncStateToken token, object? value)
    {
        // Context is not initialized
#pragma warning disable EA0011
        if (_asyncContextCurrent.Value?.Dictionary == null)
#pragma warning restore EA0011
        {
            Throw.InvalidOperationException("Context is not initialized");
        }

        _asyncContextCurrent.Value.Dictionary[token] = value;
    }

    internal int ContextCount => Volatile.Read(ref _contextCount);

    private sealed class AsyncStateHolder
    {
        public ConcurrentDictionary<AsyncStateToken, object?>? Dictionary { get; set; }
    }
}

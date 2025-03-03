// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI;

internal sealed class TestInMemoryCacheStorage : IDistributedCache
{
    private readonly ConcurrentDictionary<string, byte[]> _storage = new();

    public ICollection<string> Keys => _storage.Keys;

    public byte[]? Get(string key)
        => _storage.TryGetValue(key, out var value) ? value : null;

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => Task.FromResult(Get(key));

    public void Refresh(string key)
    {
        // In memory, nothing to refresh
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
        => Task.CompletedTask;

    public void Remove(string key)
        => _storage.TryRemove(key, out _);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _storage[key] = value;
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }
}

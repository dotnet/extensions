// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

// dummy L2 that doesn't actually store anything
internal class NullDistributedCache : IDistributedCache
{
    byte[]? IDistributedCache.Get(string key) => null;
    Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token) => Task.FromResult<byte[]?>(null);
    void IDistributedCache.Refresh(string key)
    {
        // nothing to do
    }

    Task IDistributedCache.RefreshAsync(string key, CancellationToken token) => Task.CompletedTask;
    void IDistributedCache.Remove(string key)
    {
        // nothing to do
    }

    Task IDistributedCache.RemoveAsync(string key, CancellationToken token) => Task.CompletedTask;
    void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        // nothing to do
    }

    Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token) => Task.CompletedTask;
}

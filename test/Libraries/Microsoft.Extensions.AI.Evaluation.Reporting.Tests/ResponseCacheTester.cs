// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public abstract class ResponseCacheTester
{
    private static readonly string _keyA = "A Key";
    private static readonly byte[] _responseA = Encoding.UTF8.GetBytes("Content A");
    private static readonly string _keyB = "B Key";
    private static readonly byte[] _responseB = Encoding.UTF8.GetBytes("Content B");

    internal abstract IResponseCacheProvider CreateResponseCacheProvider();
    internal abstract IResponseCacheProvider CreateResponseCacheProvider(Func<DateTime> provideDateTime);
    internal abstract bool IsConfigured { get; }

    private void SkipIfNotConfigured()
    {
        if (!IsConfigured)
        {
            throw new SkipTestException("Test not configured");
        }
    }

    [ConditionalFact]
    public async Task AddUncachedEntry()
    {
        SkipIfNotConfigured();

        string iterationName = "TestIteration";

        IResponseCacheProvider provider = CreateResponseCacheProvider();
        IDistributedCache cache = await provider.GetCacheAsync(nameof(AddUncachedEntry), iterationName);
        Assert.NotNull(cache);

        Assert.Null(await cache.GetAsync(_keyA));
        Assert.Null(cache.Get(_keyB));

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(await cache.GetAsync(_keyA) ?? []));

        cache.Set(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));
    }

    [ConditionalFact]
    public async Task RemoveCachedEntry()
    {
        SkipIfNotConfigured();

        string iterationName = "TestIteration";

        IResponseCacheProvider provider = CreateResponseCacheProvider();
        IDistributedCache cache = await provider.GetCacheAsync(nameof(RemoveCachedEntry), iterationName);
        Assert.NotNull(cache);

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(await cache.GetAsync(_keyA) ?? []));

        cache.Set(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));

        await cache.RemoveAsync(_keyA);
        Assert.Null(await cache.GetAsync(_keyA));

        cache.Remove(_keyB);
        Assert.Null(cache.Get(_keyB));
    }

    [ConditionalFact]
    public async Task CacheEntryExpiration()
    {
        SkipIfNotConfigured();

        string iterationName = "TestIteration";

        DateTime now = DateTime.UtcNow;
        DateTime provideDateTime() => now;

        IResponseCacheProvider provider = CreateResponseCacheProvider(provideDateTime);
        IDistributedCache cache = await provider.GetCacheAsync(nameof(RemoveCachedEntry), iterationName);
        Assert.NotNull(cache);

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(await cache.GetAsync(_keyA) ?? []));

        cache.Set(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));

        now = DateTime.UtcNow + DiskBasedResponseCache.CacheOptions.Default.TimeToLiveForCacheEntries;

        Assert.Null(await cache.GetAsync(_keyA));
        Assert.Null(cache.Get(_keyB));
    }

    [ConditionalFact]
    public async Task MultipleCacheInstances()
    {
        SkipIfNotConfigured();

        IResponseCacheProvider provider = CreateResponseCacheProvider();
        IDistributedCache cache = await provider.GetCacheAsync(nameof(MultipleCacheInstances), "Async");
        Assert.NotNull(cache);
        IDistributedCache cache2 = await provider.GetCacheAsync(nameof(MultipleCacheInstances), "Async");
        Assert.NotNull(cache2);

        Assert.Null(cache.Get(_keyA));

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(cache.Get(_keyA) ?? []));
        Assert.True(_responseA.SequenceEqual(cache2.Get(_keyA) ?? []));

        await cache2.SetAsync(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache2.Get(_keyB) ?? []));
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));
    }

    [ConditionalFact]
    public async Task DeleteExpiredEntries()
    {
        SkipIfNotConfigured();

        string iterationName = "TestIteration";

        DateTime now = DateTime.UtcNow;
        DateTime provideDateTime() => now;

        IResponseCacheProvider provider = CreateResponseCacheProvider(provideDateTime);
        IDistributedCache cache = await provider.GetCacheAsync(nameof(RemoveCachedEntry), iterationName);
        Assert.NotNull(cache);

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(await cache.GetAsync(_keyA) ?? []));

        cache.Set(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));

        now = DateTime.UtcNow + DiskBasedResponseCache.CacheOptions.Default.TimeToLiveForCacheEntries;

        await provider.DeleteExpiredCacheEntriesAsync();

        // Reset time back to current, to make sure the entries are actually gone
        provider = CreateResponseCacheProvider();
        cache = await provider.GetCacheAsync(nameof(RemoveCachedEntry), iterationName);
        Assert.NotNull(cache);

        Assert.Null(await cache.GetAsync(_keyA));
        Assert.Null(cache.Get(_keyB));
    }

    [ConditionalFact]
    public async Task ResetCache()
    {
        SkipIfNotConfigured();

        string iterationName = "TestIteration";

        IResponseCacheProvider provider = CreateResponseCacheProvider();
        IDistributedCache cache = await provider.GetCacheAsync(nameof(RemoveCachedEntry), iterationName);
        Assert.NotNull(cache);

        await cache.SetAsync(_keyA, _responseA);
        Assert.True(_responseA.SequenceEqual(await cache.GetAsync(_keyA) ?? []));

        cache.Set(_keyB, _responseB);
        Assert.True(_responseB.SequenceEqual(cache.Get(_keyB) ?? []));

        await provider.ResetAsync();

        Assert.Null(await cache.GetAsync(_keyA));
        Assert.Null(cache.Get(_keyB));
    }
}

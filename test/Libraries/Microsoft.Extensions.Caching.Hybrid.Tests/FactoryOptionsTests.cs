// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;
using static Microsoft.Extensions.Caching.Hybrid.Tests.DistributedCacheTests;
using static Microsoft.Extensions.Caching.Hybrid.Tests.L2Tests;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

// Covers mutations the factory makes to the HybridCacheEntryOptions it is handed —
// both Flags (replace semantics, subject to runtime-mandated floor) and non-Flags
// properties (Expiration / LocalCacheExpiration / LocalSize) that the factory writes
// directly to the options instance and which are read by SetL1 / SetL2Async / ResolveLocalSize.
public class FactoryOptionsTests(ITestOutputHelper log) : IClassFixture<TestEventListener>
{
    private static (DefaultHybridCache cache, MemoryDistributedCache localCache, ServiceProvider provider) BuildCacheWithL2(ITestOutputHelper log)
    {
        var localCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache>(new LoggingCache(log, localCache));
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return (cache, localCache, provider);
    }

    [Fact]
    public async Task FactoryCanReEnableL2Write_ThatCallerDisabled()
    {
        // Caller disabled L2 writes; factory clears the flag — value must be persisted to L2.
        var (cache, localCache, provider) = BuildCacheWithL2(log);
        using (provider)
        {
            string key = nameof(FactoryCanReEnableL2Write_ThatCallerDisabled);

            _ = await cache.GetOrCreateAsync(
                key,
                (entryOptions, _) =>
                {
                    entryOptions.Flags = HybridCacheEntryFlags.None;
                    return new ValueTask<Guid>(Guid.NewGuid());
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(1),
                    Flags = HybridCacheEntryFlags.DisableDistributedCacheWrite,
                });

            await Task.Delay(500);
            Assert.NotNull(localCache.Get(key));
        }
    }

    [Fact]
    public async Task FactoryCanDisableL2Write_ThatCallerEnabled()
    {
        // Symmetric tightening: caller allowed L2 writes (None), factory disables them.
        // Replace-semantics in ApplyFactoryOptions must make the factory's restriction stick.
        var (cache, localCache, provider) = BuildCacheWithL2(log);
        using (provider)
        {
            string key = nameof(FactoryCanDisableL2Write_ThatCallerEnabled);

            _ = await cache.GetOrCreateAsync(
                key,
                (entryOptions, _) =>
                {
                    entryOptions.Flags = HybridCacheEntryFlags.DisableDistributedCacheWrite;
                    return new ValueTask<Guid>(Guid.NewGuid());
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(1),
                    Flags = HybridCacheEntryFlags.None,
                });

            await Task.Delay(500);
            Assert.Null(localCache.Get(key));
        }
    }

    [Fact]
    public async Task FactoryCanEnableL1Write_ThatCallerDisabled()
    {
        // L1 counterpart of FactoryCanReEnableL2Write: caller passed DisableLocalCacheWrite,
        // factory clears it. If the override sticks, a subsequent read returns the same value
        // from L1 without re-invoking the factory.
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        string key = nameof(FactoryCanEnableL1Write_ThatCallerDisabled);
        int factoryCalls = 0;

        var first = await cache.GetOrCreateAsync(
            key,
            (entryOptions, _) =>
            {
                Interlocked.Increment(ref factoryCalls);
                entryOptions.Flags = HybridCacheEntryFlags.None;
                return new ValueTask<Guid>(Guid.NewGuid());
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(1),
                Flags = HybridCacheEntryFlags.DisableLocalCacheWrite,
            });

        Assert.Equal(1, factoryCalls);

        // Second call should be served from L1 — same Guid, factory not invoked again.
        var second = await cache.GetOrCreateAsync<Guid>(key, _ => new(Guid.NewGuid()));
        Assert.Equal(first, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task FactoryExpirationMutation_PropagatesToL2()
    {
        // Factory mutates Expiration; the L2 backend must receive DistributedCacheEntryOptions
        // whose AbsoluteExpirationRelativeToNow matches the factory-set value.
        var captured = new CapturingCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache>(captured);
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var factoryExpiration = TimeSpan.FromMinutes(7);
        _ = await cache.GetOrCreateAsync(
            nameof(FactoryExpirationMutation_PropagatesToL2),
            (entryOptions, _) =>
            {
                entryOptions.Expiration = factoryExpiration;
                return new ValueTask<Guid>(Guid.NewGuid());
            },
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(1) });

        await Task.Delay(500);
        Assert.Equal(factoryExpiration, captured.LastSetOptions?.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task FactoryLocalCacheExpirationMutation_ShortensL1Only()
    {
        // Factory sets LocalCacheExpiration tighter than Expiration. After the L1 entry expires
        // but before the overall entry does, the next call must re-fetch from L2 (factory not
        // invoked again), proving the factory-set L1 expiration was honored.
        var clock = new FakeTime();
        using var l1 = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var l2 = new LoggingCache(log, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions { Clock = clock })));

        var services = new ServiceCollection();
        services.AddSingleton<ISystemClock>(clock);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<IMemoryCache>(l1);
        services.AddSingleton<IDistributedCache>(l2);
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        string key = nameof(FactoryLocalCacheExpirationMutation_ShortensL1Only);
        int factoryCalls = 0;
        Func<HybridCacheEntryOptions, CancellationToken, ValueTask<Guid>> factory = (entryOptions, _) =>
        {
            Interlocked.Increment(ref factoryCalls);
            entryOptions.LocalCacheExpiration = TimeSpan.FromSeconds(30);
            return new ValueTask<Guid>(Guid.NewGuid());
        };

        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        var first = await cache.GetOrCreateAsync(key, factory, options);
        Assert.Equal(1, factoryCalls);

        // Past factory-set L1 expiration (30s) but well before overall expiration (5m).
        clock.Add(TimeSpan.FromSeconds(45));

        var second = await cache.GetOrCreateAsync(key, factory, options);

        // Same value (came from L2 round-trip), factory not invoked again.
        Assert.Equal(first, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task FactoryLocalSizeMutation_HonoredForL1SizeAccounting()
    {
        // Tight L1 SizeLimit + a payload large enough to exceed it. Without the override the
        // entry would be evicted from L1; the factory sets LocalSize = 1 to make the entry fit.
        // Verify by issuing a second call: if L1 retained the value, the factory is not
        // re-invoked and the same Guid is returned.
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = 5);
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        string key = nameof(FactoryLocalSizeMutation_HonoredForL1SizeAccounting);
        int factoryCalls = 0;

        // Use a string payload large enough that its serialized size would otherwise exceed
        // the L1 SizeLimit (the default L1 size for a string is its byte length).
        string payload = new('x', 256);

        var first = await cache.GetOrCreateAsync(
            key,
            (entryOptions, _) =>
            {
                Interlocked.Increment(ref factoryCalls);
                entryOptions.LocalSize = 1;
                return new ValueTask<string>(payload);
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(1),
                Flags = HybridCacheEntryFlags.DisableDistributedCache, // no L2; force L1-only path
            });

        Assert.Equal(1, factoryCalls);

        // Second call: served from L1 because the override kept the entry under SizeLimit.
        var second = await cache.GetOrCreateAsync<string>(key, _ => new(Guid.NewGuid().ToString()));
        Assert.Equal(first, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task FactoryMutations_DoNotLeakToCallerOptionsInstance()
    {
        // The implementation passes a clone (or fresh instance) of the caller's options to the
        // factory so that any mutations the factory performs do not bleed back into the caller's
        // shared instance. A caller that reuses the same options across many calls must see the
        // exact values it constructed.
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var callerOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(1),
            LocalCacheExpiration = TimeSpan.FromSeconds(30),
            LocalSize = 100,
            Flags = HybridCacheEntryFlags.None,
        };

        // Snapshot before
        var origExpiration = callerOptions.Expiration;
        var origLocalCacheExpiration = callerOptions.LocalCacheExpiration;
        var origLocalSize = callerOptions.LocalSize;
        var origFlags = callerOptions.Flags;

        _ = await cache.GetOrCreateAsync(
            nameof(FactoryMutations_DoNotLeakToCallerOptionsInstance),
            (entryOptions, _) =>
            {
                // Aggressively mutate everything; none of this should leak.
                Assert.NotSame(callerOptions, entryOptions);
                entryOptions.Expiration = TimeSpan.FromHours(99);
                entryOptions.LocalCacheExpiration = TimeSpan.FromHours(99);
                entryOptions.LocalSize = 9_999_999;
                entryOptions.Flags = HybridCacheEntryFlags.DisableDistributedCache | HybridCacheEntryFlags.DisableLocalCache;
                return new ValueTask<Guid>(Guid.NewGuid());
            },
            options: callerOptions);

        Assert.Equal(origExpiration, callerOptions.Expiration);
        Assert.Equal(origLocalCacheExpiration, callerOptions.LocalCacheExpiration);
        Assert.Equal(origLocalSize, callerOptions.LocalSize);
        Assert.Equal(origFlags, callerOptions.Flags);
    }

    [Fact]
    public async Task FactoryReceivesUsableOptions_WhenCallerPassedNull()
    {
        // The options-aware overload must hand the factory a real, mutable instance even when
        // the caller did not supply one — otherwise the factory cannot set entryOptions.Flags
        // etc. without a NullReferenceException, and the documented "mutate-in-factory" API
        // shape would be unusable in the common case. We observe both that the options object
        // is non-null and mutable, and that the mutation actually takes effect (no L2 write).
        var (cache, localCache, provider) = BuildCacheWithL2(log);
        using (provider)
        {
            string key = nameof(FactoryReceivesUsableOptions_WhenCallerPassedNull);
            int factoryCalls = 0;

            _ = await cache.GetOrCreateAsync<Guid>(
                key,
                (entryOptions, _) =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    Assert.NotNull(entryOptions);
                    entryOptions.Flags = HybridCacheEntryFlags.DisableDistributedCacheWrite;
                    return new ValueTask<Guid>(Guid.NewGuid());
                });

            Assert.Equal(1, factoryCalls);

            // The factory's mutation must have taken effect — no value written to L2.
            await Task.Delay(500);
            Assert.Null(localCache.Get(key));
        }
    }

    [Fact]
    public async Task FactoryLocalSize_PersistedInL2_AndReappliedOnL2Reload()
    {
        // End-to-end exercise of commit 15d649097d: the factory-set LocalSize must be persisted
        // into the L2 payload so a *different* cache instance reading from the shared L2 still
        // gets the size override applied to its L1 entry.
        //
        // Setup:
        //   Cache A: unlimited L1 SizeLimit; factory sets LocalSize=1, payload is 256 bytes.
        //   Cache B: shares L2 with A, has SizeLimit=5. On its first read it must fetch from L2,
        //     and the persisted LocalSize=1 must be reapplied to its L1 entry so the (256-byte)
        //     value fits. A second read on B then comes from L1 — observable via the LoggingCache
        //     L2 op count not increasing.
        var sharedL2 = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        string key = nameof(FactoryLocalSize_PersistedInL2_AndReappliedOnL2Reload);
        string payload = new('x', 256);

        // ---- Cache A: writes with factory-set LocalSize override ----
        var loggingA = new LoggingCache(log, sharedL2);
        var servicesA = new ServiceCollection();
        servicesA.AddMemoryCache(); // unlimited
        servicesA.AddSingleton<IDistributedCache>(loggingA);
        servicesA.AddHybridCache();
        using (var providerA = servicesA.BuildServiceProvider())
        {
            var cacheA = providerA.GetRequiredService<HybridCache>();
            _ = await cacheA.GetOrCreateAsync(
                key,
                (entryOptions, _) =>
                {
                    entryOptions.LocalSize = 1;
                    return new ValueTask<string>(payload);
                },
                options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });

            // give the background L2 write a chance to land
            await Task.Delay(500);
        }

        // Confirm the value was actually written to L2 by Cache A.
        Assert.NotNull(sharedL2.Get(key));

        // ---- Cache B: shares L2, has a tight SizeLimit that the raw payload would exceed ----
        var loggingB = new LoggingCache(log, sharedL2);
        var servicesB = new ServiceCollection();
        servicesB.AddMemoryCache(options => options.SizeLimit = 5);
        servicesB.AddSingleton<IDistributedCache>(loggingB);
        servicesB.AddHybridCache();
        using var providerB = servicesB.BuildServiceProvider();
        var cacheB = providerB.GetRequiredService<HybridCache>();

        // First call: L1 miss, fetches from L2; persisted LocalSize override must let it fit in L1.
        var firstB = await cacheB.GetOrCreateAsync<string>(key, _ => new(Guid.NewGuid().ToString()));
        Assert.Equal(payload, firstB);
        int opsAfterFirst = loggingB.OpCount;

        // Second call must hit L1 — no further L2 traffic. If the LocalSize override
        // wasn't reapplied, the entry would have been evicted from L1 immediately and
        // this call would have to re-read L2.
        var secondB = await cacheB.GetOrCreateAsync<string>(key, _ => new(Guid.NewGuid().ToString()));
        Assert.Equal(payload, secondB);
        Assert.Equal(opsAfterFirst, loggingB.OpCount);
    }

    // Test-only IDistributedCache that records the DistributedCacheEntryOptions from the
    // most recent Set/SetAsync invocation so tests can assert against it.
    private sealed class CapturingCache(IDistributedCache tail) : IDistributedCache
    {
        public DistributedCacheEntryOptions? LastSetOptions { get; private set; }

        public byte[]? Get(string key) => tail.Get(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => tail.GetAsync(key, token);
        public void Refresh(string key) => tail.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => tail.RefreshAsync(key, token);
        public void Remove(string key) => tail.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) => tail.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            LastSetOptions = options;
            tail.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            LastSetOptions = options;
            return tail.SetAsync(key, value, options, token);
        }
    }
}

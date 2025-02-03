// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Microsoft.Extensions.Caching.Hybrid.Internal.DefaultHybridCache;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class BufferReleaseTests : IClassFixture<TestEventListener> // note that buffer ref-counting is only enabled for DEBUG builds; can only verify general behaviour without that
{
    private static ServiceProvider GetDefaultCache(out DefaultHybridCache cache, Action<ServiceCollection>? config = null)
    {
        var services = new ServiceCollection();
        config?.Invoke(services);
        services.AddHybridCache();
        ServiceProvider provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Fact]
    public async Task BufferGetsReleased_NoL2()
    {
        using var provider = GetDefaultCache(out var cache);
#if DEBUG
        cache.DebugOnlyGetOutstandingBuffers(flush: true);
#endif

        var key = Me();
#if DEBUG
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif
        var first = await cache.GetOrCreateAsync(key, _ => GetAsync());
        Assert.NotNull(first);
#if DEBUG
        Assert.Equal(1, cache.DebugOnlyGetOutstandingBuffers());
#endif
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));

        // assert that we can reserve the buffer *now* (mostly to see that it behaves differently later)
        Assert.True(cacheItem.NeedsEvictionCallback, "should be pooled memory");
        Assert.True(cacheItem.TryReserveBuffer(out _));
        cacheItem.Release(); // for the above reserve

        var second = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.NotNull(second);
        Assert.NotSame(first, second);

        Assert.Equal(1, cacheItem.RefCount);
        await cache.RemoveAsync(key);
        var third = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.Null(third);

        // give it a moment for the eviction callback to kick in
        for (var i = 0; i < 10 && cacheItem.NeedsEvictionCallback; i++)
        {
            await Task.Delay(250);
        }
#if DEBUG
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif

        // assert that we can *no longer* reserve this buffer, because we've already recycled it
        Assert.False(cacheItem.TryReserveBuffer(out _));
        Assert.Equal(0, cacheItem.RefCount);
        Assert.False(cacheItem.NeedsEvictionCallback, "should be recycled now");
        static ValueTask<Customer> GetAsync() => new(new Customer { Id = 42, Name = "Fred" });
    }

    private static readonly HybridCacheEntryOptions _noUnderlying = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };

    private class TestCache : MemoryDistributedCache, IBufferDistributedCache
    {
        public TestCache(IOptions<MemoryDistributedCacheOptions> options)
            : base(options)
        {
        }

        void IBufferDistributedCache.Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options)
            => Set(key, value.ToArray(), options); // efficiency not important for this

        ValueTask IBufferDistributedCache.SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token)
            => new(SetAsync(key, value.ToArray(), options, token)); // efficiency not important for this

        bool IBufferDistributedCache.TryGet(string key, IBufferWriter<byte> destination)
            => Write(destination, Get(key));

        async ValueTask<bool> IBufferDistributedCache.TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token)
            => Write(destination, await GetAsync(key, token));

        private static bool Write(IBufferWriter<byte> destination, byte[]? buffer)
        {
            if (buffer is null)
            {
                return false;
            }

            destination.Write(buffer);
            return true;
        }
    }

    [Fact]
    public async Task BufferDoesNotNeedRelease_LegacyL2() // byte[] API; not pooled
    {
        using var provider = GetDefaultCache(out var cache,
            services => services.AddSingleton<IDistributedCache, TestCache>());

        cache.DebugRemoveFeatures(CacheFeatures.BackendBuffers);

        // prep the backend with our data
        var key = Me();
        Assert.NotNull(cache.BackendCache);
        IHybridCacheSerializer<Customer> serializer = cache.GetSerializer<Customer>();
        using (RecyclableArrayBufferWriter<byte> writer = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue))
        {
            serializer.Serialize(await GetAsync(), writer);

            var arr = ArrayPool<byte>.Shared.Rent(HybridCachePayload.GetMaxBytes(key, TagSet.Empty, writer.CommittedBytes));
            var bytes = HybridCachePayload.Write(arr, key, cache.CurrentTimestamp(), TimeSpan.FromHours(1), 0, TagSet.Empty, writer.AsSequence());
            cache.BackendCache.Set(key, new ReadOnlySpan<byte>(arr, 0, bytes).ToArray());
            ArrayPool<byte>.Shared.Return(arr);
        }
#if DEBUG
        cache.DebugOnlyGetOutstandingBuffers(flush: true);
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif
        var first = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying); // we expect this to come from L2, hence NoUnderlying
        Assert.NotNull(first);
#if DEBUG
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));

        // assert that we can reserve the buffer *now* (mostly to see that it behaves differently later)
        Assert.False(cacheItem.NeedsEvictionCallback, "should NOT be pooled memory");
        Assert.True(cacheItem.TryReserveBuffer(out _));
        cacheItem.Release(); // for the above reserve

        var second = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.NotNull(second);
        Assert.NotSame(first, second);

        Assert.Equal(1, cacheItem.RefCount);
        await cache.RemoveAsync(key);
        var third = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.Null(third);
        Assert.Null(await cache.BackendCache.GetAsync(key)); // should be gone from L2 too

        // give it a moment for the eviction callback to kick in
        for (var i = 0; i < 10 && cacheItem.NeedsEvictionCallback; i++)
        {
            await Task.Delay(250);
        }
#if DEBUG
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif

        // assert that we can *no longer* reserve this buffer, because we've already recycled it
        Assert.True(cacheItem.TryReserveBuffer(out _)); // always readable
        cacheItem.Release();
        Assert.Equal(1, cacheItem.RefCount); // not decremented because there was no need to add the hook

        Assert.False(cacheItem.NeedsEvictionCallback, "should still not need recycling");
        static ValueTask<Customer> GetAsync() => new(new Customer { Id = 42, Name = "Fred" });
    }

    [Fact]
    public async Task BufferGetsReleased_BufferL2() // IBufferWriter<byte> API; pooled
    {
        using var provider = GetDefaultCache(out var cache,
            services => services.AddSingleton<IDistributedCache, TestCache>());

        // prep the backend with our data
        var key = Me();
        Assert.NotNull(cache.BackendCache);
        IHybridCacheSerializer<Customer> serializer = cache.GetSerializer<Customer>();
        using (RecyclableArrayBufferWriter<byte> writer = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue))
        {
            serializer.Serialize(await GetAsync(), writer);

            var arr = ArrayPool<byte>.Shared.Rent(HybridCachePayload.GetMaxBytes(key, TagSet.Empty, writer.CommittedBytes));
            var bytes = HybridCachePayload.Write(arr, key, cache.CurrentTimestamp(), TimeSpan.FromHours(1), 0, TagSet.Empty, writer.AsSequence());
            cache.BackendCache.Set(key, new ReadOnlySpan<byte>(arr, 0, bytes).ToArray());
            ArrayPool<byte>.Shared.Return(arr);
        }
#if DEBUG
        cache.DebugOnlyGetOutstandingBuffers(flush: true);
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif
        var first = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying); // we expect this to come from L2, hence NoUnderlying
        Assert.NotNull(first);
#if DEBUG
        Assert.Equal(1, cache.DebugOnlyGetOutstandingBuffers());
#endif
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));

        // assert that we can reserve the buffer *now* (mostly to see that it behaves differently later)
        Assert.True(cacheItem.NeedsEvictionCallback, "should be pooled memory");
        Assert.True(cacheItem.TryReserveBuffer(out _));
        cacheItem.Release(); // for the above reserve

        var second = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.NotNull(second);
        Assert.NotSame(first, second);

        Assert.Equal(1, cacheItem.RefCount);
        await cache.RemoveAsync(key);
        var third = await cache.GetOrCreateAsync(key, _ => GetAsync(), _noUnderlying);
        Assert.Null(third);
        Assert.Null(await cache.BackendCache.GetAsync(key)); // should be gone from L2 too

        // give it a moment for the eviction callback to kick in
        for (var i = 0; i < 10 && cacheItem.NeedsEvictionCallback; i++)
        {
            await Task.Delay(250);
        }
#if DEBUG
        Assert.Equal(0, cache.DebugOnlyGetOutstandingBuffers());
#endif

        // assert that we can *no longer* reserve this buffer, because we've already recycled it
        Assert.False(cacheItem.TryReserveBuffer(out _)); // released now
        Assert.Equal(0, cacheItem.RefCount);

        Assert.False(cacheItem.NeedsEvictionCallback, "should be recycled by now");
        static ValueTask<Customer> GetAsync() => new(new Customer { Id = 42, Name = "Fred" });
    }

    [Fact]
    public void ImmutableCacheItem_Reservation()
    {
        var obj = Assert.IsType<ImmutableCacheItem<string>>(CacheItem<string>.Create(12345, TagSet.Empty));
        Assert.True(obj.DebugIsImmutable);
        obj.SetValue("abc", 3);
        Assert.False(obj.TryReserveBuffer(out _));
        Assert.True(obj.TryGetValue(NullLogger.Instance, out var value));
        Assert.Equal("abc", value);
        Assert.True(obj.TryGetSize(out var size));
        Assert.Equal(3, size);
    }

    [Fact]
    public void MutableCacheItem_Reservation()
    {
        var obj = Assert.IsType<MutableCacheItem<Customer>>(CacheItem<Customer>.Create(12345, TagSet.Empty));

        Assert.True(new DefaultJsonSerializerFactory().TryCreateSerializer<Customer>(out var serializer));
        var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize(new Customer { Id = 42, Name = "Fred" }, target);
        var arr = target.DetachCommitted(out var length);
        var chunk = new BufferChunk(arr, 0, length, true);
        target.Dispose();

        Assert.False(obj.DebugIsImmutable);
        obj.SetValue(ref chunk, serializer);

        for (int i = 0; i < 3; i++)
        {
            Assert.True(obj.TryReserveBuffer(out var lease));
            Assert.Equal(length, lease.Length);
            Assert.False(obj.Release());

            Assert.True(obj.TryGetValue(NullLogger.Instance, out var value));
            Assert.Equal(42, value.Id);
            Assert.Equal("Fred", value.Name);

            Assert.True(obj.TryGetSize(out var size));
            Assert.Equal(length, size);
        }

        Assert.True(obj.Release());
        Assert.False(obj.TryReserveBuffer(out _));
        Assert.False(obj.TryGetValue(NullLogger.Instance, out _));
        Assert.False(obj.TryGetSize(out var _));
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class L2Tests(ITestOutputHelper log) : IClassFixture<TestEventListener>
{
    private static string CreateString(bool work = false)
    {
        Assert.True(work, "we didn't expect this to be invoked");
        return Guid.NewGuid().ToString();
    }

    private static readonly HybridCacheEntryOptions _expiry = new() { Expiration = TimeSpan.FromMinutes(3.5) };

    private static readonly HybridCacheEntryOptions _expiryNoL1 = new() { Flags = HybridCacheEntryFlags.DisableLocalCache, Expiration = TimeSpan.FromMinutes(3.5) };

    private ITestOutputHelper Log => log;

    private class Options<T>(T value) : IOptions<T>
        where T : class
    {
        T IOptions<T>.Value => value;
    }

    private ServiceProvider GetDefaultCache(bool buffers, out DefaultHybridCache cache, bool disableLocalCacheSerialization = false)
    {
        var services = new ServiceCollection();
        var localCacheOptions = new Options<MemoryDistributedCacheOptions>(new());
        var localCache = new MemoryDistributedCache(localCacheOptions);
        services.AddSingleton<IDistributedCache>(buffers ? new BufferLoggingCache(Log, localCache) : new LoggingCache(Log, localCache));
        services.AddHybridCache(options => options.DisableLocalCacheSerialization = disableLocalCacheSerialization);
        ServiceProvider provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssertL2Operations_Immutable(bool buffers)
    {
        using var provider = GetDefaultCache(buffers, out var cache);
        var backend = Assert.IsAssignableFrom<LoggingCache>(cache.BackendCache);
        Log.WriteLine("Inventing key...");
        var s = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString(true)));
        Assert.Equal(3, backend.OpCount); // (wildcard timstamp GET), GET, SET

        Log.WriteLine("Reading with L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()));
            Assert.Equal(s, x);
            Assert.Same(s, x);
        }

        Assert.Equal(3, backend.OpCount); // shouldn't be hit

        Log.WriteLine("Reading without L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()), _expiryNoL1);
            Assert.Equal(s, x);
            Assert.NotSame(s, x);
        }

        Assert.Equal(8, backend.OpCount); // should be read every time

        Log.WriteLine("Setting value directly");
        s = CreateString(true);
        await cache.SetAsync(Me(), s);
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()));
            Assert.Equal(s, x);
            Assert.Same(s, x);
        }

        Assert.Equal(9, backend.OpCount); // SET

        Log.WriteLine("Removing key...");
        await cache.RemoveAsync(Me());
        Assert.Equal(10, backend.OpCount); // DEL

        Log.WriteLine("Fetching new...");
        var t = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString(true)));
        Assert.NotEqual(s, t);
        Assert.Equal(12, backend.OpCount); // GET, SET
    }

    public sealed class Foo
    {
        public string Value { get; set; } = "";
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssertL2Operations_Mutable(bool buffers)
    {
        using var provider = GetDefaultCache(buffers, out var cache);
        var backend = Assert.IsAssignableFrom<LoggingCache>(cache.BackendCache);
        Log.WriteLine("Inventing key...");
        var s = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString(true) }), _expiry);
        Assert.Equal(3, backend.OpCount); // (wildcard timstamp GET), GET, SET

        Log.WriteLine("Reading with L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }), _expiry);
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }

        Assert.Equal(3, backend.OpCount); // shouldn't be hit

        Log.WriteLine("Reading without L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }), _expiryNoL1);
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }

        Assert.Equal(8, backend.OpCount); // should be read every time

        Log.WriteLine("Setting value directly");
        s = new Foo { Value = CreateString(true) };
        await cache.SetAsync(Me(), s);
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }), _expiry);
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }

        Assert.Equal(9, backend.OpCount); // SET

        Log.WriteLine("Removing key...");
        await cache.RemoveAsync(Me());
        Assert.Equal(10, backend.OpCount); // DEL

        Log.WriteLine("Fetching new...");
        var t = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString(true) }), _expiry);
        Assert.NotEqual(s.Value, t.Value);
        Assert.Equal(12, backend.OpCount); // GET, SET
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisableLocalCacheSerialization_ReturnsSameMutableInstance(bool buffers)
    {
        using var provider = GetDefaultCache(buffers, out var cache, disableLocalCacheSerialization: true);
        var backend = Assert.IsAssignableFrom<LoggingCache>(cache.BackendCache);

        Foo original = await cache.GetOrCreateAsync(Me(), _ => new ValueTask<Foo>(new Foo { Value = "value" }));
        Foo fromL1 = await cache.GetOrCreateAsync(Me(), _ => new ValueTask<Foo>(new Foo { Value = "unused" }));

        Assert.Same(original, fromL1);

        cache.LocalCache.Remove(Me());
        Foo fromL2 = await cache.GetOrCreateAsync(Me(), _ => new ValueTask<Foo>(new Foo { Value = "unused" }));
        Foo fromL1AfterL2 = await cache.GetOrCreateAsync(Me(), _ => new ValueTask<Foo>(new Foo { Value = "unused" }));

        Assert.NotSame(original, fromL2);
        Assert.Same(fromL2, fromL1AfterL2);
        Assert.Equal(4, backend.OpCount); // wildcard timestamp GET, GET, SET, GET
    }

    [Fact]
    public async Task DisableLocalCacheSerialization_DoesNotSerializeWhenCacheWritesAreDisabled()
    {
        var services = new ServiceCollection();
        services.AddHybridCache(options => options.DisableLocalCacheSerialization = true);
        services.AddSingleton<IHybridCacheSerializer<Foo>>(new ThrowingFooSerializer());
        using ServiceProvider provider = services.BuildServiceProvider();
        HybridCache cache = provider.GetRequiredService<HybridCache>();
        var value = new Foo { Value = "value" };
        var options = new HybridCacheEntryOptions
        {
            Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite,
        };

        Foo actual = await cache.GetOrCreateAsync(Me(), _ => new ValueTask<Foo>(value), options);

        Assert.Same(value, actual);
    }

    private sealed class ThrowingFooSerializer : IHybridCacheSerializer<Foo>
    {
        public Foo Deserialize(ReadOnlySequence<byte> source) => throw new NotSupportedException();

        public void Serialize(Foo value, IBufferWriter<byte> target) => throw new NotSupportedException();
    }

    private class BufferLoggingCache : LoggingCache, IBufferDistributedCache
    {
        public BufferLoggingCache(ITestOutputHelper log, IDistributedCache tail)
            : base(log, tail)
        {
        }

        void IBufferDistributedCache.Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"Set (ROS-byte): {key}");
            Tail.Set(key, value.ToArray(), options);
        }

        ValueTask IBufferDistributedCache.SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"SetAsync (ROS-byte): {key}");
            return new(Tail.SetAsync(key, value.ToArray(), options, token));
        }

        bool IBufferDistributedCache.TryGet(string key, IBufferWriter<byte> destination)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"TryGet: {key}");
            var buffer = Tail.Get(key);
            if (buffer is null)
            {
                return false;
            }

            destination.Write(buffer);
            return true;
        }

        async ValueTask<bool> IBufferDistributedCache.TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"TryGetAsync: {key}");
            var buffer = await Tail.GetAsync(key, token);
            if (buffer is null)
            {
                return false;
            }

            destination.Write(buffer);
            return true;
        }
    }

    internal class LoggingCache(ITestOutputHelper log, IDistributedCache tail) : IDistributedCache
    {
        protected ITestOutputHelper Log => log;
        protected IDistributedCache Tail => tail;

        protected int ProtectedOpCount;

        public int OpCount => Volatile.Read(ref ProtectedOpCount);

        byte[]? IDistributedCache.Get(string key)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"Get: {key}");
            return Tail.Get(key);
        }

        Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"GetAsync: {key}");
            return Tail.GetAsync(key, token);
        }

        void IDistributedCache.Refresh(string key)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"Refresh: {key}");
            Tail.Refresh(key);
        }

        Task IDistributedCache.RefreshAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"RefreshAsync: {key}");
            return Tail.RefreshAsync(key, token);
        }

        void IDistributedCache.Remove(string key)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"Remove: {key}");
            Tail.Remove(key);
        }

        Task IDistributedCache.RemoveAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"RemoveAsync: {key}");
            return Tail.RemoveAsync(key, token);
        }

        void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"Set (byte[]): {key} (expiry: {options.AbsoluteExpirationRelativeToNow})");
            Tail.Set(key, value, options);
        }

        Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
        {
            Interlocked.Increment(ref ProtectedOpCount);
            Log.WriteLine($"SetAsync (byte[]): {key} (expiry: {options.AbsoluteExpirationRelativeToNow})");
            return Tail.SetAsync(key, value, options, token);
        }
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}

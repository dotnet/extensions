// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
using static Microsoft.Extensions.Caching.Hybrid.Tests.L2Tests;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class LocalInvalidationTests(ITestOutputHelper log)
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
    public async Task GlobalInvalidateNoTags()
    {
        using var services = GetDefaultCache(out var cache);
        var value = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()));

        // should work immediately as-is
        Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid())));

        // invalidating a normal tag should have no effect
        await cache.RemoveByTagAsync("foo");
        Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid())));

        // invalidating everything should force a re-fetch
        await cache.RemoveByTagAsync("*");
        var newValue = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()));
        Assert.NotEqual(value, newValue);

        // which should now be repeatable again
        Assert.Equal(newValue, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid())));
    }

    private static class Options
    {
        public static IOptions<T> Create<T>(T value)
            where T : class
            => new OptionsImpl<T>(value);

        private sealed class OptionsImpl<T> : IOptions<T>
            where T : class
        {
            public OptionsImpl(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TagBasedInvalidate(bool withL2)
    {
        using IMemoryCache l1 = new MemoryCache(new MemoryCacheOptions());
        IDistributedCache? l2 = null;
        if (withL2)
        {
            MemoryDistributedCacheOptions options = new();
            MemoryDistributedCache mdc = new(Options.Create(options));
            l2 = new LoggingCache(log, mdc);
        }

        Guid lastValue = Guid.Empty;

        // loop because we want to test pre-existing L1/L2 impact
        for (int i = 0; i < 3; i++)
        {
            using var services = GetDefaultCache(out var cache, svc =>
            {
                svc.AddSingleton(l1);
                if (l2 is not null)
                {
                    svc.AddSingleton(l2);
                }
            });
            var clock = services.GetRequiredService<TimeProvider>();

            string[] tags = ["abc"];
            var value = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);
            log.WriteLine($"First value: {value}");
            if (lastValue != Guid.Empty)
            {
                Assert.Equal(lastValue, value);
            }

            // should work immediately as-is
            Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));

            // invalidating a normal tag should have no effect
            await cache.RemoveByTagAsync("foo");
            Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));

            // invalidating a tag we have should force a re-fetch
            await cache.RemoveByTagAsync("abc");
            var newValue = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);
            log.WriteLine($"Value after invalidating tag abc: {value}");
            Assert.NotEqual(value, newValue);

            // which should now be repeatable again
            Assert.Equal(newValue, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));
            value = newValue;

            // invalidating everything should force a re-fetch
            await cache.RemoveByTagAsync("*");
            newValue = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);
            log.WriteLine($"Value after invalidating tag *: {value}");
            Assert.NotEqual(value, newValue);

            // which should now be repeatable again
            Assert.Equal(newValue, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));
            lastValue = newValue;

            var now = clock.GetTimestamp();
            do
            {
                await Task.Delay(10);
            }
            while (clock.GetTimestamp() == now);
        }
    }
}

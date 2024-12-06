// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class LocalInvalidationTests
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

    [Fact]
    public async Task TagBasedInvalidate()
    {
        using var services = GetDefaultCache(out var cache);
        string[] tags = ["abc"];
        var value = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);

        // should work immediately as-is
        Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));

        // invalidating a normal tag should have no effect
        await cache.RemoveByTagAsync("foo");
        Assert.Equal(value, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));

        // invalidating a tag we have should force a re-fetch
        await cache.RemoveByTagAsync("abc");
        var newValue = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);
        Assert.NotEqual(value, newValue);

        // which should now be repeatable again
        Assert.Equal(newValue, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));
        value = newValue;

        // invalidating everything should force a re-fetch
        await cache.RemoveByTagAsync("*");
        newValue = await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags);
        Assert.NotEqual(value, newValue);

        // which should now be repeatable again
        Assert.Equal(newValue, await cache.GetOrCreateAsync<Guid>("abc", ct => new(Guid.NewGuid()), tags: tags));
    }
}

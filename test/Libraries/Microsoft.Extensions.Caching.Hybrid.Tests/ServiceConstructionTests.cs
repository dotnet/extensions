// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#if NET9_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8769 // Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class ServiceConstructionTests : IClassFixture<TestEventListener>
{
    [Fact]
    public void CanCreateDefaultService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
    }

    [Fact]
    public void CanCreateServiceWithManualOptions()
    {
        var services = new ServiceCollection();
        services.AddHybridCache(options =>
        {
            options.MaximumKeyLength = 937;
            options.DefaultEntryOptions = new() { Expiration = TimeSpan.FromSeconds(120), Flags = HybridCacheEntryFlags.DisableLocalCacheRead };
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        var obj = Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
        var options = obj.Options;
        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.Expiration);
        Assert.Equal(HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Null(defaults.LocalCacheExpiration); // wasn't specified
    }

    [Fact]
    public void CanCreateServiceWithKeyedDistributedCache()
    {
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache1>(typeof(CustomMemoryDistributedCache1));
        services.AddHybridCache(options => options.DistributedCacheServiceKey = typeof(CustomMemoryDistributedCache1));

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybrid = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        var hybridOptions = hybrid.Options;

        var backend = Assert.IsType<CustomMemoryDistributedCache1>(hybrid.BackendCache);
        Assert.Same(typeof(CustomMemoryDistributedCache1), hybridOptions.DistributedCacheServiceKey);
        Assert.Same(backend, provider.GetRequiredKeyedService<IDistributedCache>(typeof(CustomMemoryDistributedCache1)));
    }

    [Fact]
    public void ThrowsWhenDistributedCacheKeyNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddHybridCache(options => options.DistributedCacheServiceKey = typeof(CustomMemoryDistributedCache1));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(provider.GetRequiredService<HybridCache>);
    }

    [Fact]
    public void ThrowsWhenRegisteredDistributedCacheIsNotKeyed()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddHybridCache(options => options.DistributedCacheServiceKey = typeof(CustomMemoryDistributedCache1));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(provider.GetRequiredService<HybridCache>);
    }

    [Fact]
    public void CanCreateKeyedHybridCacheServiceWithNullKey()
    {
        var services = new ServiceCollection();
        services.AddKeyedHybridCache(null);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Resolves using null key registration
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(null));

        // Resolves as the non-keyed registration
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
    }

    [Fact]
    public void CanCreateKeyedServicesWithStringKeys()
    {
        var services = new ServiceCollection();
        services.AddKeyedHybridCache("one");
        services.AddKeyedHybridCache("two");
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("one"));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("two"));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(null));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
    }

    [Fact]
    public void CanCreateKeyedServicesWithStringKeysAndSetupActions()
    {
        var services = new ServiceCollection();
        services.AddKeyedHybridCache("one", options => options.MaximumKeyLength = 1);
        services.AddKeyedHybridCache("two", options => options.MaximumKeyLength = 2);
        services.AddKeyedHybridCache(null, options => options.MaximumKeyLength = 3);
        using ServiceProvider provider = services.BuildServiceProvider();

        var one = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("one"));
        Assert.Equal(1, one.Options.MaximumKeyLength);

        var two = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("two"));
        Assert.Equal(2, two.Options.MaximumKeyLength);

        var threeKeyed = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(null));
        Assert.Equal(3, threeKeyed.Options.MaximumKeyLength);

        var threeUnkeyed = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        Assert.Equal(3, threeUnkeyed.Options.MaximumKeyLength);
    }

    [Fact]
    public void CanCreateKeyedServicesWithTypeKeys()
    {
        var services = new ServiceCollection();
        services.AddKeyedHybridCache(typeof(string));
        services.AddKeyedHybridCache(typeof(int));
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(string)));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(int)));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(null));
        Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
    }

    [Fact]
    public void CanCreateKeyedServicesWithTypeKeysAndSetupActions()
    {
        var services = new ServiceCollection();
        services.AddKeyedHybridCache(typeof(string), options => options.MaximumKeyLength = 1);
        services.AddKeyedHybridCache(typeof(int), options => options.MaximumKeyLength = 2);
        services.AddKeyedHybridCache(null, options => options.MaximumKeyLength = 3);

        using ServiceProvider provider = services.BuildServiceProvider();
        var one = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(string)));
        Assert.Equal(1, one.Options.MaximumKeyLength);

        var two = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(int)));
        Assert.Equal(2, two.Options.MaximumKeyLength);

        var threeKeyed = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(null));
        Assert.Equal(3, threeKeyed.Options.MaximumKeyLength);

        var threeUnkeyed = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        Assert.Equal(3, threeUnkeyed.Options.MaximumKeyLength);
    }

    [Fact]
    public void CanCreateKeyedServicesWithKeyedDistributedCaches()
    {
        var services = new ServiceCollection();
        services.TryAddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache1>(typeof(CustomMemoryDistributedCache1));
        services.TryAddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache2>(typeof(CustomMemoryDistributedCache2));

        services.AddKeyedHybridCache("one", options => options.DistributedCacheServiceKey = typeof(CustomMemoryDistributedCache1));
        services.AddKeyedHybridCache("two", options => options.DistributedCacheServiceKey = typeof(CustomMemoryDistributedCache2));
        using ServiceProvider provider = services.BuildServiceProvider();

        var cacheOne = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("one"));
        var cacheOneOptions = cacheOne.Options;
        var cacheOneBackend = Assert.IsType<CustomMemoryDistributedCache1>(cacheOne.BackendCache);
        Assert.Same(typeof(CustomMemoryDistributedCache1), cacheOneOptions.DistributedCacheServiceKey);
        Assert.Same(cacheOneBackend, provider.GetRequiredKeyedService<IDistributedCache>(typeof(CustomMemoryDistributedCache1)));

        var cacheTwo = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("two"));
        var cacheTwoOptions = cacheTwo.Options;
        var cacheTwoBackend = Assert.IsType<CustomMemoryDistributedCache2>(cacheTwo.BackendCache);
        Assert.Same(typeof(CustomMemoryDistributedCache2), cacheTwoOptions.DistributedCacheServiceKey);
        Assert.Same(cacheTwoBackend, provider.GetRequiredKeyedService<IDistributedCache>(typeof(CustomMemoryDistributedCache2)));
    }

    [Fact]
    public async Task KeyedHybridCaches_ShareLocalMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = 2);
        services.AddSingleton<IDistributedCache, CustomMemoryDistributedCache1>();
        services.AddKeyedHybridCache("hybrid1");
        services.AddKeyedHybridCache("hybrid2");
        services.AddKeyedHybridCache("hybrid3");

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybrid1 = provider.GetRequiredKeyedService<HybridCache>("hybrid1");
        var hybrid2 = provider.GetRequiredKeyedService<HybridCache>("hybrid2");
        var hybrid3 = provider.GetRequiredKeyedService<HybridCache>("hybrid3");

        await hybrid1.SetAsync("entry1", 1);
        await hybrid2.SetAsync("entry2", 2);
        await hybrid3.SetAsync("entry3", 3);

        var localCache = provider.GetRequiredService<IMemoryCache>();
        Assert.True(localCache.TryGetValue("entry1", out object? _));
        Assert.True(localCache.TryGetValue("entry2", out object? _));

        // The third item fails to be cached locally because of the shared local cache size limit
        Assert.False(localCache.TryGetValue("entry3", out object? _));

        // But we can still get it from the hybrid cache (which gets it from the distributed cache)
        var actual3 = await hybrid3.GetOrCreateAsync<int>("entry3", ct =>
        {
            Assert.Fail("Should not be called as the item should be found in the distributed cache");
            return new ValueTask<int>(-1);
        });

        Assert.Equal(3, actual3);
    }

    [Fact]
    public void CanCreateRedisAndSqlServerBackedHybridCaches()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDistributedCache, RedisCache>("Redis");

        services.AddKeyedSingleton<IDistributedCache, SqlServerCache>("SqlServer",
            (sp, key) => new SqlServerCache(new SqlServerCacheOptions
            {
                ConnectionString = "test",
                SchemaName = "test",
                TableName = "test"
            }));

        services.AddKeyedHybridCache("HybridWithRedis", options => options.DistributedCacheServiceKey = "Redis");
        services.AddKeyedHybridCache("HybridWithSqlServer", options => options.DistributedCacheServiceKey = "SqlServer");

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybridWithRedis = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("HybridWithRedis"));
        var hybridWithRedisBackend = Assert.IsType<RedisCache>(hybridWithRedis.BackendCache);
        Assert.Same(hybridWithRedisBackend, provider.GetRequiredKeyedService<IDistributedCache>("Redis"));

        var hybridWithSqlServer = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("HybridWithSqlServer"));
        var hybridWithSqlServerBackend = Assert.IsType<SqlServerCache>(hybridWithSqlServer.BackendCache);
        Assert.Same(hybridWithSqlServerBackend, provider.GetRequiredKeyedService<IDistributedCache>("SqlServer"));
    }

#if NET9_0_OR_GREATER // for Bind API
    [Fact]
    public void CanParseOptions_NoEntryOptions()
    {
        var configBuilder = new ConfigurationBuilder();

        configBuilder.AddInMemoryCollection([
            new("no_entry_options:MaximumKeyLength", "937")
        ]);

        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "no_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        Assert.Null(options.DefaultEntryOptions);
    }

    [Fact]
    public void CanParseOptions_WithEntryOptions() // in particular, check we can parse the timespan and [Flags] enums
    {
        var configBuilder = new ConfigurationBuilder();

        configBuilder.AddInMemoryCollection([
            new("with_entry_options:MaximumKeyLength", "937"),
            new("with_entry_options:DefaultEntryOptions:Flags", "DisableCompression, DisableLocalCacheRead"),
            new("with_entry_options:DefaultEntryOptions:LocalCacheExpiration", "00:02:00")
        ]);

        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "with_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(HybridCacheEntryFlags.DisableCompression | HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.LocalCacheExpiration);
        Assert.Null(defaults.Expiration); // wasn't specified
    }

    [Fact]
    public void CanCreateKeyedServicesWithKeyedDistributedCaches_UsingNamedOptions()
    {
        var configBuilder = new ConfigurationBuilder();

        configBuilder.AddInMemoryCollection([
            new("HybridOne:DistributedCacheServiceKey", "DistributedOne"),
            new("HybridTwo:DistributedCacheServiceKey", "DistributedTwo")
        ]);

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache1>("DistributedOne");
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache2>("DistributedTwo");
        services.AddOptions<HybridCacheOptions>("HybridOne").Configure(options => ConfigurationBinder.Bind(config, "HybridOne", options));
        services.AddOptions<HybridCacheOptions>("HybridTwo").Configure(options => ConfigurationBinder.Bind(config, "HybridTwo", options));
        services.AddKeyedHybridCache(typeof(CustomMemoryDistributedCache1), "HybridOne");
        services.AddKeyedHybridCache(typeof(CustomMemoryDistributedCache2), "HybridTwo");

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybridOne = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1)));
        var hybridOneOptions = hybridOne.Options;
        var hybridOneBackend = Assert.IsType<CustomMemoryDistributedCache1>(hybridOne.BackendCache);
        Assert.Equal("DistributedOne", hybridOneOptions.DistributedCacheServiceKey);

        var hybridTwo = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2)));
        var hybridTwoOptions = hybridTwo.Options;
        var hybridTwoBackend = Assert.IsType<CustomMemoryDistributedCache2>(hybridTwo.BackendCache);
        Assert.Equal("DistributedTwo", hybridTwoOptions.DistributedCacheServiceKey);

        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));

        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
    }

    [Fact]
    public void CanCreateKeyedServicesWithKeyedDistributedCaches_UsingSetupActions()
    {
        var configBuilder = new ConfigurationBuilder();

        configBuilder.AddInMemoryCollection([
            new("HybridOne:DistributedCacheServiceKey", "DistributedOne"),
            new("HybridTwo:DistributedCacheServiceKey", "DistributedTwo")
        ]);

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache1>("DistributedOne");
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache2>("DistributedTwo");
        services.AddKeyedHybridCache("HybridOne", options => ConfigurationBinder.Bind(config, "HybridOne", options));
        services.AddKeyedHybridCache("HybridTwo", options => ConfigurationBinder.Bind(config, "HybridTwo", options));

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybridOne = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("HybridOne"));
        var hybridOneOptions = hybridOne.Options;
        var hybridOneBackend = Assert.IsType<CustomMemoryDistributedCache1>(hybridOne.BackendCache);
        Assert.Equal("DistributedOne", hybridOneOptions.DistributedCacheServiceKey);

        var hybridTwo = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>("HybridTwo"));
        var hybridTwoOptions = hybridTwo.Options;
        var hybridTwoBackend = Assert.IsType<CustomMemoryDistributedCache2>(hybridTwo.BackendCache);
        Assert.Equal("DistributedTwo", hybridTwoOptions.DistributedCacheServiceKey);

        provider.GetRequiredKeyedService<HybridCache>("HybridOne");
        provider.GetRequiredKeyedService<HybridCache>("HybridOne");
        provider.GetRequiredKeyedService<HybridCache>("HybridOne");

        provider.GetRequiredKeyedService<HybridCache>("HybridTwo");
        provider.GetRequiredKeyedService<HybridCache>("HybridTwo");
        provider.GetRequiredKeyedService<HybridCache>("HybridTwo");
    }

    [Fact]
    public void CanCreateKeyedServicesWithKeyedDistributedCaches_UsingNamedOptionsAndSetupActions()
    {
        var configBuilder = new ConfigurationBuilder();

        configBuilder.AddInMemoryCollection([
            new("HybridOne:DistributedCacheServiceKey", "DistributedOne"),
            new("HybridTwo:DistributedCacheServiceKey", "DistributedTwo")
        ]);

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache1>("DistributedOne");
        services.AddKeyedSingleton<IDistributedCache, CustomMemoryDistributedCache2>("DistributedTwo");
        services.AddKeyedHybridCache(typeof(CustomMemoryDistributedCache1), "HybridOne", options => ConfigurationBinder.Bind(config, "HybridOne", options));
        services.AddKeyedHybridCache(typeof(CustomMemoryDistributedCache2), "HybridTwo", options => ConfigurationBinder.Bind(config, "HybridTwo", options));

        using ServiceProvider provider = services.BuildServiceProvider();
        var hybridOne = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1)));
        var hybridOneOptions = hybridOne.Options;
        var hybridOneBackend = Assert.IsType<CustomMemoryDistributedCache1>(hybridOne.BackendCache);
        Assert.Equal("DistributedOne", hybridOneOptions.DistributedCacheServiceKey);

        var hybridTwo = Assert.IsType<DefaultHybridCache>(provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2)));
        var hybridTwoOptions = hybridTwo.Options;
        var hybridTwoBackend = Assert.IsType<CustomMemoryDistributedCache2>(hybridTwo.BackendCache);
        Assert.Equal("DistributedTwo", hybridTwoOptions.DistributedCacheServiceKey);

        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache1));

        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
        provider.GetRequiredKeyedService<HybridCache>(typeof(CustomMemoryDistributedCache2));
    }
#endif

    [Fact]
    public async Task BasicStatelessUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), async _ => expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task BasicStatefulUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), expected, async (state, _) => state);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DefaultSerializerConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<InbuiltTypeSerializer>(cache.GetSerializer<string>());
        Assert.IsType<InbuiltTypeSerializer>(cache.GetSerializer<byte[]>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Customer>>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Fact]
    public void CustomSerializerConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache().AddSerializer<Customer, CustomerSerializer>();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<CustomerSerializer>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Fact]
    public void CustomSerializerFactoryConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache().AddSerializerFactory<CustomFactory>();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<CustomerSerializer>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultMemoryDistributedCacheIsIgnored(bool manual)
    {
        var services = new ServiceCollection();
        if (manual)
        {
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.Null(cache.BackendCache);
    }

    [Fact]
    public void SubclassMemoryDistributedCacheIsNotIgnored()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache, CustomMemoryDistributedCache1>();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.NotNull(cache.BackendCache);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SubclassMemoryCacheIsNotIgnored(bool manual)
    {
        var services = new ServiceCollection();
        if (manual)
        {
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<IMemoryCache, CustomMemoryCache>();
        services.AddHybridCache();
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.NotNull(cache.BackendCache);
    }

    [Theory]

    // first 4 tests; regardless of which options objects are supplied, since nothing specified: defaults are assumed
    [InlineData(false, null, null, null, false, null, null, null)]
    [InlineData(true, null, null, null, false, null, null, null)]
    [InlineData(false, null, null, null, true, null, null, null)]
    [InlineData(true, null, null, null, true, null, null, null)]

    // flags; per-item wins, without merge
    [InlineData(false, null, null, null, true, null, null, HybridCacheEntryFlags.None)]
    [InlineData(false, null, null, null, true, null, null, HybridCacheEntryFlags.DisableLocalCacheRead, null, null, HybridCacheEntryFlags.DisableLocalCacheRead)]
    [InlineData(true, null, null, HybridCacheEntryFlags.None, true, null, null, HybridCacheEntryFlags.DisableLocalCacheRead, null, null, HybridCacheEntryFlags.DisableLocalCacheRead)]
    [InlineData(true, null, null, HybridCacheEntryFlags.DisableLocalCacheWrite, true, null, null, HybridCacheEntryFlags.DisableLocalCacheRead, null, null, HybridCacheEntryFlags.DisableLocalCacheRead)]

    // flags; global wins if per-item omits, or no per-item flags
    [InlineData(true, null, null, HybridCacheEntryFlags.DisableLocalCacheWrite, true, null, null, null, null, null, HybridCacheEntryFlags.DisableLocalCacheWrite)]
    [InlineData(true, null, null, HybridCacheEntryFlags.DisableLocalCacheWrite, false, null, null, null, null, null, HybridCacheEntryFlags.DisableLocalCacheWrite)]

    // local expiration; per-item wins; expiration bleeds into local expiration (but not the other way around)
    [InlineData(false, null, null, null, true, 42, null, null, 42, 42)]
    [InlineData(false, null, null, null, true, 43, 42, null, 43, 42)]
    [InlineData(false, null, null, null, true, null, 43, null, null, 43)]

    // global expiration; expiration bleeds into local expiration (but not the other way around)
    [InlineData(true, 42, null, null, false, null, null, null, 42, 42)]
    [InlineData(true, 43, 42, null, false, null, null, null, 43, 42)]
    [InlineData(true, null, 43, null, false, null, null, null, null, 43)]

    // both expirations specified; expiration bleeds into local expiration (but not the other way around)
    [InlineData(true, 43, 42, null, true, null, null, null, 43, 42)]
    [InlineData(true, 43, 42, null, true, 44, null, null, 44, 44)]
    [InlineData(true, 43, 42, null, true, 45, 44, null, 45, 44)]
    [InlineData(true, 43, 42, null, true, null, 45, null, 43, 45)]

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Most pragmatic and readable way of expressing multiple scenarios.")]
    public void VerifyCacheEntryOptionsScenarios(
        bool defaultsSpecified, int? defaultExpiration, int? defaultLocalCacheExpiration, HybridCacheEntryFlags? defaultFlags,
        bool perItemSpecified, int? perItemExpiration, int? perItemLocalCacheExpiration, HybridCacheEntryFlags? perItemFlags,
        int? expectedExpiration = null, int? expectedLocalCacheExpiration = null, HybridCacheEntryFlags expectedFlags = HybridCacheEntryFlags.None)
    {
        expectedFlags |= HybridCacheEntryFlags.DisableDistributedCache; // hard flag because no L2 present

        var services = new ServiceCollection();
        services.AddHybridCache(options =>
        {
            if (defaultsSpecified)
            {
                options.DefaultEntryOptions = new()
                {
                    Expiration = defaultExpiration is null ? null : TimeSpan.FromMinutes(defaultExpiration.GetValueOrDefault()),
                    LocalCacheExpiration = defaultLocalCacheExpiration is null ? null : TimeSpan.FromMinutes(defaultLocalCacheExpiration.GetValueOrDefault()),
                    Flags = defaultFlags,
                };
            }
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        HybridCacheEntryOptions? itemOptions = null;
        if (perItemSpecified)
        {
            itemOptions = new()
            {
                Expiration = perItemExpiration is null ? null : TimeSpan.FromMinutes(perItemExpiration.GetValueOrDefault()),
                LocalCacheExpiration = perItemLocalCacheExpiration is null ? null : TimeSpan.FromMinutes(perItemLocalCacheExpiration.GetValueOrDefault()),
                Flags = perItemFlags,
            };
        }

        Assert.Equal(expectedFlags, cache.GetEffectiveFlags(itemOptions));
        Assert.Equal(TimeSpan.FromMinutes(expectedExpiration ?? DefaultHybridCache.DefaultExpirationMinutes), cache.GetL2AbsoluteExpirationRelativeToNow(itemOptions));
        Assert.Equal(TimeSpan.FromMinutes(expectedLocalCacheExpiration ?? DefaultHybridCache.DefaultExpirationMinutes), cache.GetL1AbsoluteExpirationRelativeToNow(itemOptions));
    }

    private class CustomMemoryCache : MemoryCache
    {
        public CustomMemoryCache(IOptions<MemoryCacheOptions> options)
            : base(options)
        {
        }

        public CustomMemoryCache(IOptions<MemoryCacheOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory)
        {
        }
    }

    internal class CustomMemoryDistributedCache1 : MemoryDistributedCache
    {
        public CustomMemoryDistributedCache1(IOptions<MemoryDistributedCacheOptions> options)
            : base(options)
        {
        }

        public CustomMemoryDistributedCache1(IOptions<MemoryDistributedCacheOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory)
        {
        }
    }

    internal class CustomMemoryDistributedCache2 : MemoryDistributedCache
    {
        public CustomMemoryDistributedCache2(IOptions<MemoryDistributedCacheOptions> options)
            : base(options)
        {
        }

        public CustomMemoryDistributedCache2(IOptions<MemoryDistributedCacheOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory)
        {
        }
    }

    private class Customer
    {
    }

    private class Order
    {
    }

    private class CustomerSerializer : IHybridCacheSerializer<Customer>
    {
        Customer IHybridCacheSerializer<Customer>.Deserialize(ReadOnlySequence<byte> source) => throw new NotSupportedException();
        void IHybridCacheSerializer<Customer>.Serialize(Customer value, IBufferWriter<byte> target) => throw new NotSupportedException();
    }

    private class CustomFactory : IHybridCacheSerializerFactory
    {
        bool IHybridCacheSerializerFactory.TryCreateSerializer<T>(out IHybridCacheSerializer<T>? serializer)
        {
            if (typeof(T) == typeof(Customer))
            {
                serializer = (IHybridCacheSerializer<T>)new CustomerSerializer();
                return true;
            }

            serializer = null;
            return false;
        }
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}

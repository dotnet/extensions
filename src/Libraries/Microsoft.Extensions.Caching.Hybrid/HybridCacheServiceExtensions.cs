// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configuration extension methods for <see cref="HybridCache"/>.
/// </summary>
public static class HybridCacheServiceExtensions
{
    /// <summary>
    /// Adds support for multi-tier caching services.
    /// </summary>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> system.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> setupAction)
    {
        _ = Throw.IfNull(setupAction);
        _ = AddHybridCache(services);
        _ = services.Configure(setupAction);
        return new HybridCacheBuilder(services);
    }

    /// <summary>
    /// Adds support for multi-tier caching services.
    /// </summary>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> system.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        _ = services.AddOptions().AddMemoryCache();
        services.TryAddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.TryAddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.TryAddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);
        services.TryAddSingleton<HybridCache, DefaultHybridCache>();
        return new HybridCacheBuilder(services);
    }

    /// <summary>
    /// Adds support for keyed multi-tier caching services.
    /// </summary>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> system.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, string name, Action<HybridCacheOptions> setupAction)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(setupAction);

        // Register options for this key
        _ = services.AddOptions<HybridCacheOptions>(name).Configure(setupAction);

        // Register keyed in-memory cache
        services.TryAddKeyedSingleton<IMemoryCache, MemoryCache>(name);

        // Register keyed HybridCache
        services.TryAddKeyedSingleton<HybridCache>(name, (sp, _) =>
            new DefaultHybridCache(
                Options.Options.Create(sp.GetRequiredService<IOptionsMonitor<HybridCacheOptions>>().Get(name)),
                sp,
                sp.GetKeyedService<IMemoryCache>(name),
                sp.GetKeyedService<IDistributedCache>(name)));

        services.TryAddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.TryAddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.TryAddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);

        return new HybridCacheBuilder(services);
    }
}

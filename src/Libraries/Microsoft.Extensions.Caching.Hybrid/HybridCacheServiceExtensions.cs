// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid.Internal;
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
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="setupAction">A delegate to run to configure the <see cref="HybridCacheOptions"/> instance.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> setupAction)
    {
        _ = Throw.IfNull(setupAction);

        var builder = AddHybridCache(services);
        _ = services.Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Adds support for multi-tier caching services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        var builder = PrepareServices(services);

        services.TryAddSingleton<HybridCache, DefaultHybridCache>();

        return builder;
    }

    /// <summary>
    /// Adds support for multi-tier caching services with a keyed registration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The key for the service registration.</param>
    /// <param name="setupAction">A delegate to run to configure the <see cref="HybridCacheOptions"/> instance.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddKeyedHybridCache(this IServiceCollection services, object? serviceKey, Action<HybridCacheOptions> setupAction) =>
        AddKeyedHybridCache(services, serviceKey, serviceKey?.ToString() ?? Options.Options.DefaultName, setupAction);

    /// <summary>
    /// Adds support for multi-tier caching services with a keyed registration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The key for the service registration.</param>
    /// <param name="optionsName">The named options name to use for the <see cref="HybridCacheOptions"/> instance.</param>
    /// <param name="setupAction">A delegate to run to configure the <see cref="HybridCacheOptions"/> instance.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddKeyedHybridCache(this IServiceCollection services, object? serviceKey, string optionsName, Action<HybridCacheOptions> setupAction)
    {
        _ = Throw.IfNull(setupAction);

        var builder = AddKeyedHybridCache(services, serviceKey, optionsName);
        _ = services.AddOptions<HybridCacheOptions>(optionsName).Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Adds support for multi-tier caching services with a keyed registration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The key for the service registration.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddKeyedHybridCache(this IServiceCollection services, object? serviceKey) =>
        AddKeyedHybridCache(services, serviceKey, serviceKey?.ToString() ?? Options.Options.DefaultName);

    /// <summary>
    /// Adds support for multi-tier caching services with a keyed registration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The key for the service registration.</param>
    /// <param name="optionsName">The named options name to use for the <see cref="HybridCacheOptions"/> instance.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    public static IHybridCacheBuilder AddKeyedHybridCache(this IServiceCollection services, object? serviceKey, string optionsName)
    {
        _ = Throw.IfNull(optionsName);

        var builder = PrepareServices(services);
        _ = services.AddOptions<HybridCacheOptions>(optionsName);

        _ = services.AddKeyedSingleton<HybridCache, DefaultHybridCache>(serviceKey, (sp, key) =>
        {
            var optionsService = sp.GetRequiredService<IOptionsMonitor<HybridCacheOptions>>();
            var options = optionsService.Get(optionsName);

            return new DefaultHybridCache(options, sp);
        });

        return builder;
    }

    /// <summary>
    /// Adds the services required for hybrid caching.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to prepare with <see cref="HybridCache"/> prerequisites.</param>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> service.</returns>
    private static HybridCacheBuilder PrepareServices(IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        _ = services.AddOptions().AddMemoryCache();
        services.TryAddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.TryAddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.TryAddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);

        return new HybridCacheBuilder(services);
    }
}

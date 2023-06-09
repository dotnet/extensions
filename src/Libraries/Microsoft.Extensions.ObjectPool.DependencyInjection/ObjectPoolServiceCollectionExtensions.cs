// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding <see cref="ObjectPool{T}"/> to DI container.
/// </summary>
[Experimental]
public static class ObjectPoolServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="ObjectPool{TService}"/> and lets DI return scoped instances of TService.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">The action used to configure the options of the pool.</param>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is <c>Environment.ProcessorCount * 2</c>.
    /// The pooled type instances are obtainable by resolving <see cref="ObjectPool{TService}"/> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(
        this IServiceCollection services,
        Action<PoolOptions>? configure = null)
        where TService : class
    {
        return services.AddPooledInternal<TService, TService>(configure);
    }

    /// <summary>
    /// Adds an <see cref="ObjectPool{TService}"/> and let DI return scoped instances of TService.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Configuration of the pool.</param>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is <c>Environment.ProcessorCount * 2</c>.
    /// The pooled type instances are obtainable by resolving <see cref="ObjectPool{TService}"/> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        Action<PoolOptions>? configure = null)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddPooledInternal<TService, TImplementation>(configure);
    }

    /// <summary>
    /// Registers an action used to configure the <see cref="PoolOptions"/> of a typed pool.
    /// </summary>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePool<TService>(this IServiceCollection services, Action<PoolOptions> configure)
        where TService : class
    {
        return services.Configure<PoolOptions>(typeof(TService).FullName, configure);
    }

    private static IServiceCollection AddPooledInternal<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        Action<PoolOptions>? configure)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);

        if (configure != null)
        {
            // Register a PoolOption instance specific to the type
            _ = services.ConfigurePool<TService>(configure);
        }

        return services
            .AddSingleton<ObjectPool<TService>>(provider =>
            {
                var options = provider.GetService<IOptionsFactory<PoolOptions>>()?.Create(typeof(TService).FullName!) ?? new PoolOptions();

                return new DefaultObjectPool<TService>(new DependencyInjectionPooledObjectPolicy<TService, TImplementation>(provider), options.Capacity);
            });
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Extension methods for adding <see cref="ObjectPool{T}"/> to DI container.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = WarningDefinitions.SharedUrlFormat)]
public static class ObjectPoolServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="ObjectPool{TService}"/> and lets DI return scoped instances of TService.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">The action used to configure the options of the pool.</param>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable by resolving <see cref="ObjectPool{TService}"/> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(
        this IServiceCollection services,
        Action<DependencyInjectionPoolOptions>? configure = null)
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
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable by resolving <see cref="ObjectPool{TService}"/> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        Action<DependencyInjectionPoolOptions>? configure = null)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddPooledInternal<TService, TImplementation>(configure);
    }

    /// <summary>
    /// Registers an action used to configure the <see cref="DependencyInjectionPoolOptions"/> of a typed pool.
    /// </summary>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePool<TService>(this IServiceCollection services, Action<DependencyInjectionPoolOptions> configure)
        where TService : class
    {
        return services.Configure<DependencyInjectionPoolOptions>(typeof(TService).FullName, configure);
    }

    /// <summary>
    /// Configures DI pools.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(DependencyInjectionPoolOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IServiceCollection ConfigurePools(this IServiceCollection services, IConfigurationSection section)
    {
        foreach (var child in Throw.IfNull(section).GetChildren())
        {
            if (!int.TryParse(child.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity))
            {
                Throw.ArgumentException(nameof(section), $"Can't parse '{child.Key}' value '{child.Value}' to integer.");
            }

            _ = services.Configure<DependencyInjectionPoolOptions>(child.Key, options => options.Capacity = capacity);
        }

        return services;
    }

    private static IServiceCollection AddPooledInternal<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        Action<DependencyInjectionPoolOptions>? configure)
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
                var options = provider.GetService<IOptionsFactory<DependencyInjectionPoolOptions>>()?.Create(typeof(TService).FullName!) ?? new DependencyInjectionPoolOptions();

                return new DefaultObjectPool<TService>(new DependencyInjectionPooledObjectPolicy<TService, TImplementation>(provider), options.Capacity);
            });
    }
}

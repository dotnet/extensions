// Assembly 'Microsoft.Extensions.ObjectPool.DependencyInjection'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Extension methods for adding <see cref="T:Microsoft.Extensions.ObjectPool.ObjectPool`1" /> to DI container.
/// </summary>
[Experimental("EXTEXP0010", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ObjectPoolServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.ObjectPool.ObjectPool`1" /> and lets DI return scoped instances of TService.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="configure">The action used to configure the options of the pool.</param>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable by resolving <see cref="T:Microsoft.Extensions.ObjectPool.ObjectPool`1" /> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services, Action<DependencyInjectionPoolOptions>? configure = null) where TService : class;

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.ObjectPool.ObjectPool`1" /> and let DI return scoped instances of TService.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="configure">Configuration of the pool.</param>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable by resolving <see cref="T:Microsoft.Extensions.ObjectPool.ObjectPool`1" /> from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection services, Action<DependencyInjectionPoolOptions>? configure = null) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Registers an action used to configure the <see cref="T:Microsoft.Extensions.ObjectPool.DependencyInjectionPoolOptions" /> of a typed pool.
    /// </summary>
    /// <typeparam name="TService">The type of objects to pool.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePool<TService>(this IServiceCollection services, Action<DependencyInjectionPoolOptions> configure) where TService : class;

    /// <summary>
    /// Configures DI pools.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePools(this IServiceCollection services, IConfigurationSection section);
}

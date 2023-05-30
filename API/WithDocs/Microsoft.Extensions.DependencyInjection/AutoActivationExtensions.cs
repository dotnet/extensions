// Assembly 'Microsoft.Extensions.DependencyInjection.AutoActivation'

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for automatically activating singletons after application starts.
/// </summary>
public static class AutoActivationExtensions
{
    /// <summary>
    /// Enforces singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection Activate<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation using the factory specified in implementationFactory
    /// to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in TService with a factory specified
    /// in implementationFactory to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in TService to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType to the specified
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType);

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with an implementation
    /// of the type specified in implementationType to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType to the specified
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType);

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType with an implementation
    /// of the type specified in implementationType to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in TService
    /// to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation using the factory specified in implementationFactory
    /// to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
}

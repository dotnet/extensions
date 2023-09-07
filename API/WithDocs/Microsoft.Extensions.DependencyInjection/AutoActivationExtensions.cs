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
    /// <typeparam name="TService">The type of the service to activate.</typeparam>
    /// <param name="services">The service collection containing the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection ActivateSingleton<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Enforces singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <param name="services">The service collection containing the service.</param>
    /// <param name="serviceType">The type of the service to activate.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection ActivateSingleton(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType);

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType to the specified
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType);

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType);

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;

    /// <summary>
    /// Enforces keyed singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <typeparam name="TService">The type of the service to activate.</typeparam>
    /// <param name="services">The service collection containing the service.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection ActivateKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;

    /// <summary>
    /// Enforces keyed singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to activate.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection ActivateKeyedSingleton(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, object? serviceKey);

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory) where TService : class;

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    [Experimental("EXTEXP0012", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static void TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory) where TService : class;
}

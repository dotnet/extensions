// Assembly 'Microsoft.Extensions.DependencyInjection.AutoActivation'

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

public static class AutoActivationExtensions
{
    public static IServiceCollection ActivateSingleton<TService>(this IServiceCollection services) where TService : class;
    public static IServiceCollection ActivateSingleton(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType);
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType);
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
    public static IServiceCollection ActivateKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;
    public static IServiceCollection ActivateKeyedSingleton(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, object? serviceKey);
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory) where TService : class;
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);
    public static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);
    public static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);
    public static void TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey) where TService : class;
    public static void TryAddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService;
    public static void TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory) where TService : class;
}

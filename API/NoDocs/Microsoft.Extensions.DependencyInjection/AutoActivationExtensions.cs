// Assembly 'Microsoft.Extensions.DependencyInjection.AutoActivation'

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

public static class AutoActivationExtensions
{
    public static IServiceCollection Activate<TService>(this IServiceCollection services) where TService : class;
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType);
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services) where TService : class;
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
}

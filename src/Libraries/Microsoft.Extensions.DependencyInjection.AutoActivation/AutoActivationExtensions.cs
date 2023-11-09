// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for automatically activating singletons after application starts.
/// </summary>
public static partial class AutoActivationExtensions
{
    /// <summary>
    /// Enforces singleton activation at startup time rather than at runtime.
    /// </summary>
    /// <typeparam name="TService">The type of the service to activate.</typeparam>
    /// <param name="services">The service collection containing the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection ActivateSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        _ = Throw.IfNull(services);

        _ = services
            .AddHostedService<AutoActivationHostedService>()
            .AddOptions<AutoActivatorOptions>()
            .Configure(ao =>
            {
                var constructed = typeof(IEnumerable<TService>);
                if (ao.AutoActivators.Contains(constructed))
                {
                    return;
                }

                if (ao.AutoActivators.Remove(typeof(TService)))
                {
                    _ = ao.AutoActivators.Add(constructed);
                    return;
                }

                _ = ao.AutoActivators.Add(typeof(TService));
            });

        return services;
    }

    /// <summary>
    /// Enforces singleton activation at startup time rather than at runtime.
    /// </summary>
    /// <param name="services">The service collection containing the service.</param>
    /// <param name="serviceType">The type of the service to activate.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAccessedMembers]")]
    public static IServiceCollection ActivateSingleton(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        _ = services
            .AddHostedService<AutoActivationHostedService>()
            .AddOptions<AutoActivatorOptions>()
            .Configure(ao =>
            {
                var constructed = typeof(IEnumerable<>).MakeGenericType(serviceType);
                if (ao.AutoActivators.Contains(constructed))
                {
                    return;
                }

                if (ao.AutoActivators.Remove(serviceType))
                {
                    _ = ao.AutoActivators.Add(constructed);
                    return;
                }

                _ = ao.AutoActivators.Add(serviceType);
            });

        return services;
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        return services
            .AddSingleton<TService, TImplementation>(implementationFactory)
            .ActivateSingleton<TService>();
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);

        return services
            .AddSingleton<TService, TImplementation>()
            .ActivateSingleton<TService>();
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        return services
            .AddSingleton<TService>(implementationFactory)
            .ActivateSingleton<TService>();
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        _ = Throw.IfNull(services);

        return services
            .AddSingleton<TService>()
            .ActivateSingleton<TService>();
    }

    /// <summary>
    /// Adds an auto-activated singleton service of the type specified in serviceType to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        return services
            .AddSingleton(serviceType)
            .ActivateSingleton(serviceType);
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationFactory);

        return services
            .AddSingleton(serviceType, implementationFactory)
            .ActivateSingleton(serviceType);
    }

    /// <summary>
    /// Adds an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationType);

        return services
            .AddSingleton(serviceType, implementationType)
            .ActivateSingleton(serviceType);
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(serviceType, serviceType));
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationType);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(serviceType, implementationType));
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationFactory);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(serviceType, implementationFactory));
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivate<TService>(ServiceDescriptor.Singleton<TService, TService>());
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivate<TService>(ServiceDescriptor.Singleton<TService, TImplementation>());
    }

    /// <summary>
    /// Tries to add an auto-activated singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        services.TryAddAndActivate<TService>(ServiceDescriptor.Singleton<TService>(implementationFactory));
    }

    private static void TryAddAndActivate<TService>(this IServiceCollection services, ServiceDescriptor descriptor)
        where TService : class
    {
        if (services.Any(d => d.ServiceType == descriptor.ServiceType && d.ServiceKey == descriptor.ServiceKey))
        {
            return;
        }

        services.Add(descriptor);
        _ = services.ActivateSingleton<TService>();
    }

    private static void TryAddAndActivate(this IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (services.Any(d => d.ServiceType == descriptor.ServiceType && d.ServiceKey == descriptor.ServiceKey))
        {
            return;
        }

        services.Add(descriptor);
        _ = services.ActivateSingleton(descriptor.ServiceType);
    }
}

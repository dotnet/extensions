// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AutoActivationExtensions
{
    /// <summary>
    /// Enforces keyed singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <typeparam name="TService">The type of the service to activate.</typeparam>
    /// <param name="services">The service collection containing the service.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection ActivateKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
    {
        _ = Throw.IfNull(services);

        _ = services
            .AddHostedService<AutoActivationHostedService>()
            .AddOptions<AutoActivatorOptions>()
            .Configure(ao =>
            {
                var constructed = typeof(IEnumerable<TService>);
                if (ao.KeyedAutoActivators.Contains((constructed, serviceKey)))
                {
                    return;
                }

                if (ao.KeyedAutoActivators.Remove((typeof(TService), serviceKey)))
                {
                    _ = ao.KeyedAutoActivators.Add((constructed, serviceKey));
                    return;
                }

                _ = ao.KeyedAutoActivators.Add((typeof(TService), serviceKey));
            });

        return services;
    }

    /// <summary>
    /// Enforces keyed singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to activate.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAccessedMembers]")]
    public static IServiceCollection ActivateKeyedSingleton(
        this IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType,
        object? serviceKey)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        _ = services
            .AddHostedService<AutoActivationHostedService>()
            .AddOptions<AutoActivatorOptions>()
            .Configure(ao =>
            {
                var constructed = typeof(IEnumerable<>).MakeGenericType(serviceType);
                if (ao.KeyedAutoActivators.Contains((constructed, serviceKey)))
                {
                    return;
                }

                if (ao.KeyedAutoActivators.Remove((serviceType, serviceKey)))
                {
                    _ = ao.KeyedAutoActivators.Add((constructed, serviceKey));
                    return;
                }

                _ = ao.KeyedAutoActivators.Add((serviceType, serviceKey));
            });

        return services;
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        return services
            .AddKeyedSingleton<TService, TImplementation>(serviceKey, implementationFactory)
            .ActivateKeyedSingleton<TService>(serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        return services
            .AddKeyedSingleton<TService, TImplementation>(serviceKey)
            .ActivateKeyedSingleton<TService>(serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        return services
            .AddKeyedSingleton<TService>(serviceKey, implementationFactory)
            .ActivateKeyedSingleton<TService>(serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
    {
        return services
            .AddKeyedSingleton<TService>(serviceKey)
            .ActivateKeyedSingleton<TService>(serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        return services
            .AddKeyedSingleton(serviceType, serviceKey)
            .ActivateKeyedSingleton(serviceType, serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey,
        Func<IServiceProvider, object?, object> implementationFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationFactory);

        return services
            .AddKeyedSingleton(serviceType, serviceKey, implementationFactory)
            .ActivateKeyedSingleton(serviceType, serviceKey);
    }

    /// <summary>
    /// Adds an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey,
        Type implementationType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationType);

        return services
            .AddKeyedSingleton(serviceType, serviceKey, implementationType)
            .ActivateKeyedSingleton(serviceType, serviceKey);
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    public static void TryAddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        services.TryAddAndActivateKeyed(ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, serviceType));
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    public static void TryAddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey,
        Type implementationType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationType);

        services.TryAddAndActivateKeyed(ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, implementationType));
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    public static void TryAddActivatedKeyedSingleton(
        this IServiceCollection services,
        Type serviceType,
        object? serviceKey,
        Func<IServiceProvider, object?, object> implementationFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationFactory);

        services.TryAddAndActivateKeyed(ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, implementationFactory));
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivateKeyed<TService>(ServiceDescriptor.KeyedSingleton<TService, TService>(serviceKey));
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    public static void TryAddActivatedKeyedSingleton<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivateKeyed<TService>(ServiceDescriptor.KeyedSingleton<TService, TImplementation>(serviceKey));
    }

    /// <summary>
    /// Tries to add an auto-activated keyed singleton service.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceKey">An object used to uniquely identify the specific service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        services.TryAddAndActivateKeyed<TService>(ServiceDescriptor.KeyedSingleton<TService>(serviceKey, implementationFactory));
    }

    private static void TryAddAndActivateKeyed<TService>(this IServiceCollection services, ServiceDescriptor descriptor)
        where TService : class
    {
        if (services.Any(d => d.ServiceType == descriptor.ServiceType && d.ServiceKey == descriptor.ServiceKey))
        {
            return;
        }

        services.Add(descriptor);
        _ = services.ActivateKeyedSingleton<TService>(descriptor.ServiceKey);
    }

    private static void TryAddAndActivateKeyed(this IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (services.Any(d => d.ServiceType == descriptor.ServiceType && d.ServiceKey == descriptor.ServiceKey))
        {
            return;
        }

        services.Add(descriptor);
        _ = services.ActivateKeyedSingleton(descriptor.ServiceType, descriptor.ServiceKey);
    }
}

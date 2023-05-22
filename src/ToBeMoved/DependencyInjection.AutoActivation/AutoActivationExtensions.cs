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
public static class AutoActivationExtensions
{
    /// <summary>
    /// Enforces singleton activation at startup time rather then at runtime.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection Activate<TService>(this IServiceCollection services)
        where TService : class
    {
        _ = Throw.IfNull(services);

        return services.Activate(typeof(TService));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation using the factory specified in implementationFactory
    /// to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        return services.AddSingleton<TService, TImplementation>(implementationFactory).Activate(typeof(TService));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService with a factory specified
    /// in implementationFactory to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        return services.AddActivatedSingleton(typeof(TService), implementationFactory);
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        return services.AddActivatedSingleton(typeof(TService));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        return services.AddSingleton(serviceType).Activate(serviceType);
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddActivatedSingleton(typeof(TService), typeof(TImplementation));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationFactory);

        return services.AddSingleton(serviceType, implementationFactory).Activate(serviceType);
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with an implementation
    /// of the type specified in implementationType to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);
        _ = Throw.IfNull(implementationType);

        return services.AddSingleton(serviceType, implementationType).Activate(serviceType);
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType to the specified
    /// <see cref="IServiceCollection"/> if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    public static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceType);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(serviceType, serviceType));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with an implementation
    /// of the type specified in implementationType to the specified <see cref="IServiceCollection"/>
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
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
    /// Adds an autoactivated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="IServiceCollection"/>
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
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
    /// Adds an autoactivated singleton service of the type specified in TService
    /// to the specified <see cref="IServiceCollection"/>
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(typeof(TService), typeof(TService)));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in TService with an implementation
    /// type specified in TImplementation using the factory specified in implementationFactory
    /// to the specified <see cref="IServiceCollection"/>
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    public static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        _ = Throw.IfNull(services);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(typeof(TService), typeof(TImplementation)));
    }

    /// <summary>
    /// Adds an autoactivated singleton service of the type specified in serviceType with a factory
    /// specified in implementationFactory to the specified <see cref="IServiceCollection"/>
    /// if the service type hasn't already been registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    public static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(implementationFactory);

        services.TryAddAndActivate(ServiceDescriptor.Singleton(typeof(TService), implementationFactory));
    }

    private static void TryAddAndActivate(this IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (services.Any(d => d.ServiceType == descriptor.ServiceType))
        {
            return;
        }

        services.Add(descriptor);
        _ = services.Activate(descriptor.ServiceType);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAccessedMembers]")]
    private static IServiceCollection Activate(this IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType)
    {
        _ = services.AddHostedServiceIfNotExist()
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

    private static IServiceCollection AddHostedServiceIfNotExist(this IServiceCollection services)
    {
#if NETFRAMEWORK
        var autoActivationHostedServiceType = typeof(AutoActivationHostedService);

        // This loop is needed only for older .NET versions where there's no check
        // if the service was already added to the IServiceCollection.
        foreach (var service in services)
        {
            if (service.ImplementationType == autoActivationHostedServiceType)
            {
                return services;
            }
        }
#endif
        return services.AddHostedService<AutoActivationHostedService>();
    }
}

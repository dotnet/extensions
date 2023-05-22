// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for adding named services to <see cref="INamedServiceProvider{T}"/>.
/// </summary>
public static class NamedServiceCollectionExtensions
{
    /// <summary>
    /// Adds a singleton named service of the type specific in <typeparamref name="TService"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="name">The name of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddNamedSingleton<TService>(this IServiceCollection serviceCollection,
        string name, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNullOrEmpty(name);

        _ = serviceCollection.AddOptions<NamedServiceProviderOptions<TService>>(name)
            .Configure(options =>
                options.Services.Add(NamedServiceDescriptor<TService>.Describe(
                    implementationFactory,
                    ServiceLifetime.Singleton)));

        serviceCollection.TryAdd(ServiceDescriptor.Singleton(typeof(INamedServiceProvider<>), typeof(NamedServiceProvider<>)));

        return serviceCollection;
    }

    /// <summary>
    /// Adds a singleton named service of the type specific in <typeparamref name="TService"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="name">The name of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddNamedSingleton<TService, TImplementation>(this IServiceCollection serviceCollection,
        string name)
        where TService : class
        where TImplementation : TService
    {
        return serviceCollection.AddNamedSingleton<TService>(name,
            provider => ActivatorUtilities.CreateInstance<TImplementation>(provider, Array.Empty<object>()));
    }

    /// <summary>
    /// Adds a singleton named service of the type specific in <typeparamref name="TService"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="name">The name of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddNamedSingleton<TService>(this IServiceCollection serviceCollection,
        string name)
        where TService : class
    {
        return serviceCollection.AddNamedSingleton<TService, TService>(name);
    }

    /// <summary>
    /// Adds a transient named service of the type specific in <typeparamref name="TService"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="name">The name of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddNamedTransient<TService>(this IServiceCollection serviceCollection, string name)
        where TService : class
    {
        return serviceCollection.AddNamedTransient<TService>(name,
            provider => ActivatorUtilities.CreateInstance<TService>(provider, Array.Empty<object>()));
    }

    /// <summary>
    /// Adds a transient named service of the type specific in <typeparamref name="TService"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="name">The name of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddNamedTransient<TService>(this IServiceCollection serviceCollection,
        string name, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNullOrEmpty(name);

        _ = serviceCollection.AddOptions<NamedServiceProviderOptions<TService>>(name)
            .Configure(options =>
                options.Services.Add(NamedServiceDescriptor<TService>.Describe(
                    implementationFactory,
                    ServiceLifetime.Transient)));

        serviceCollection.TryAdd(ServiceDescriptor.Singleton(typeof(INamedServiceProvider<>), typeof(NamedServiceProvider<>)));

        return serviceCollection;
    }
}

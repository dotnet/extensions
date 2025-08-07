// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IImageClient"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental("MEAI001")]
public static class ImageClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IImageClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ImageClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ImageClientBuilder AddImageClient(
        this IServiceCollection serviceCollection,
        IImageClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddImageClient(serviceCollection, _ => innerClient, lifetime);

    /// <summary>Registers a singleton <see cref="IImageClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ImageClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ImageClientBuilder AddImageClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IImageClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ImageClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IImageClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="IImageClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ImageClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/>, <paramref name="serviceKey"/>, or <paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ImageClientBuilder AddKeyedImageClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        IImageClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedImageClient(serviceCollection, serviceKey, _ => innerClient, lifetime);

    /// <summary>Registers a keyed singleton <see cref="IImageClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ImageClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/>, <paramref name="serviceKey"/>, or <paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ImageClientBuilder AddKeyedImageClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        Func<IServiceProvider, IImageClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ImageClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IImageClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

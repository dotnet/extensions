// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IChatClient"/> with a <see cref="IServiceCollection"/>.</summary>
public static class ChatClientBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TChatClient">The type of the chat client to register.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TChatClient>(
        this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TChatClient : class, IChatClient
        => AddChatClient(serviceCollection, provider => ActivatorUtilities.CreateInstance<TChatClient>(provider), lifetime);

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        IChatClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddChatClient(serviceCollection, _ => innerClient, lifetime);

    /// <summary>Registers an <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IChatClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IChatClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>
    /// Registers a keyed <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TChatClient">The type of the chat client to register.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TChatClient>(
        this IServiceCollection serviceCollection,
        object serviceKey,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TChatClient : class, IChatClient
        => AddKeyedChatClient(serviceCollection, serviceKey, provider => ActivatorUtilities.CreateInstance<TChatClient>(provider), lifetime);

    /// <summary>Registers a keyed <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        IChatClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedChatClient(serviceCollection, serviceKey, _ => innerClient, lifetime);

    /// <summary>Registers a keyed <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        Func<IServiceProvider, IChatClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IChatClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

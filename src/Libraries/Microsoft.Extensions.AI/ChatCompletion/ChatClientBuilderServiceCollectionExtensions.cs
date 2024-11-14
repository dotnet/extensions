// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IChatClient"/> with a <see cref="IServiceCollection"/>.</summary>
public static class ChatClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        IChatClient innerClient)
        => AddChatClient(serviceCollection, _ => innerClient);

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="T">The type of the inner <see cref="IChatClient"/> that represents the underlying backend. This will be resolved from the service provider.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient<T>(
        this IServiceCollection serviceCollection)
        where T : IChatClient
        => AddChatClient(serviceCollection, services => services.GetRequiredService<T>());

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IChatClient> innerClientFactory)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        _ = serviceCollection.AddSingleton(builder.Build);
        return builder;
    }

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        IChatClient innerClient)
        => AddKeyedChatClient(serviceCollection, serviceKey, _ => innerClient);

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="T">The type of the inner <see cref="IChatClient"/> that represents the underlying backend. This will be resolved from the service provider.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient<T>(
        this IServiceCollection serviceCollection,
        object serviceKey)
        where T : IChatClient
        => AddKeyedChatClient(serviceCollection, serviceKey, services => services.GetRequiredService<T>());

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        Func<IServiceProvider, IChatClient> innerClientFactory)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        _ = serviceCollection.AddKeyedSingleton(serviceKey, (services, _) => builder.Build(services));
        return builder;
    }
}

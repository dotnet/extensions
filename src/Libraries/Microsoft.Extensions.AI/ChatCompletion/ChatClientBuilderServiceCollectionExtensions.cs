// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for registering <see cref="IChatClient"/> with a <see cref="IServiceCollection"/>.</summary>
public static class ChatClientBuilderServiceCollectionExtensions
{
    /// <summary>Adds a chat client to the <see cref="IServiceCollection"/>.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="clientFactory">The factory to use to construct the <see cref="IChatClient"/> instance.</param>
    /// <returns>The <paramref name="services"/> collection.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static IServiceCollection AddChatClient(
        this IServiceCollection services,
        Func<ChatClientBuilder, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(clientFactory);

        return services.AddScoped(services =>
            clientFactory(new ChatClientBuilder(services)));
    }

    /// <summary>Adds a chat client to the <see cref="IServiceCollection"/>.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="clientFactory">The factory to use to construct the <see cref="IChatClient"/> instance.</param>
    /// <returns>The <paramref name="services"/> collection.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static IServiceCollection AddKeyedChatClient(
        this IServiceCollection services,
        object serviceKey,
        Func<ChatClientBuilder, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(clientFactory);

        return services.AddKeyedScoped(serviceKey, (services, _) =>
            clientFactory(new ChatClientBuilder(services)));
    }
}

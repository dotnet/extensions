// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="ISpeechToTextClient"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental("MEAI001")]
public static class SpeechToTextClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="ISpeechToTextClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="SpeechToTextClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static SpeechToTextClientBuilder AddSpeechToTextClient(
        this IServiceCollection serviceCollection,
        ISpeechToTextClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddSpeechToTextClient(serviceCollection, _ => innerClient, lifetime);

    /// <summary>Registers a singleton <see cref="ISpeechToTextClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="SpeechToTextClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static SpeechToTextClientBuilder AddSpeechToTextClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, ISpeechToTextClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new SpeechToTextClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(ISpeechToTextClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="ISpeechToTextClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="SpeechToTextClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static SpeechToTextClientBuilder AddKeyedSpeechToTextClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        ISpeechToTextClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedSpeechToTextClient(serviceCollection, serviceKey, _ => innerClient, lifetime);

    /// <summary>Registers a keyed singleton <see cref="ISpeechToTextClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="SpeechToTextClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    public static SpeechToTextClientBuilder AddKeyedSpeechToTextClient(
        this IServiceCollection serviceCollection,
        object serviceKey,
        Func<IServiceProvider, ISpeechToTextClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(serviceKey);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new SpeechToTextClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(ISpeechToTextClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

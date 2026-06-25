// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IOcrClient"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OcrClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IOcrClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IOcrClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="OcrClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    public static OcrClientBuilder AddOcrClient(
        this IServiceCollection serviceCollection,
        IOcrClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddOcrClient(serviceCollection, _ => innerClient, lifetime);

    /// <summary>Registers a singleton <see cref="IOcrClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IOcrClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="OcrClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    public static OcrClientBuilder AddOcrClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IOcrClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new OcrClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IOcrClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="IOcrClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IOcrClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="OcrClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    public static OcrClientBuilder AddKeyedOcrClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IOcrClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedOcrClient(serviceCollection, serviceKey, _ => innerClient, lifetime);

    /// <summary>Registers a keyed singleton <see cref="IOcrClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IOcrClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="OcrClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    public static OcrClientBuilder AddKeyedOcrClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IOcrClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new OcrClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IOcrClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IVideoGenerator"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class VideoGeneratorBuilderServiceCollectionExtensions
{
    /// <summary>Registers an <see cref="IVideoGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGenerator">The inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="VideoGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered with the specified <paramref name="lifetime"/>.</remarks>
    public static VideoGeneratorBuilder AddVideoGenerator(
        this IServiceCollection serviceCollection,
        IVideoGenerator innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddVideoGenerator(serviceCollection, _ => innerGenerator, lifetime);

    /// <summary>Registers an <see cref="IVideoGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="VideoGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered with the specified <paramref name="lifetime"/>.</remarks>
    public static VideoGeneratorBuilder AddVideoGenerator(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IVideoGenerator> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new VideoGeneratorBuilder(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IVideoGenerator), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed <see cref="IVideoGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="innerGenerator">The inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="VideoGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered with the specified <paramref name="lifetime"/>.</remarks>
    public static VideoGeneratorBuilder AddKeyedVideoGenerator(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IVideoGenerator innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedVideoGenerator(serviceCollection, serviceKey, _ => innerGenerator, lifetime);

    /// <summary>Registers a keyed <see cref="IVideoGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="VideoGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered with the specified <paramref name="lifetime"/>.</remarks>
    public static VideoGeneratorBuilder AddKeyedVideoGenerator(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IVideoGenerator> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new VideoGeneratorBuilder(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IVideoGenerator), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}

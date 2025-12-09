// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IImageGenerator"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental(DiagnosticIds.Experiments.ImageGeneration, Message = DiagnosticIds.Experiments.ImageGenerationMessage, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ImageGeneratorBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IImageGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGenerator">The inner <see cref="IImageGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="ImageGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static ImageGeneratorBuilder AddImageGenerator(
        this IServiceCollection serviceCollection,
        IImageGenerator innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddImageGenerator(serviceCollection, _ => innerGenerator, lifetime);

    /// <summary>Registers a singleton <see cref="IImageGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IImageGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="ImageGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> or <paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    public static ImageGeneratorBuilder AddImageGenerator(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IImageGenerator> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new ImageGeneratorBuilder(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IImageGenerator), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="IImageGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="innerGenerator">The inner <see cref="IImageGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="ImageGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/>, <paramref name="serviceKey"/>, or <paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered as a scoped service.</remarks>
    public static ImageGeneratorBuilder AddKeyedImageGenerator(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IImageGenerator innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedImageGenerator(serviceCollection, serviceKey, _ => innerGenerator, lifetime);

    /// <summary>Registers a keyed singleton <see cref="IImageGenerator"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IImageGenerator"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="ImageGeneratorBuilder"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/>, <paramref name="serviceKey"/>, or <paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>The generator is registered as a scoped service.</remarks>
    public static ImageGeneratorBuilder AddKeyedImageGenerator(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IImageGenerator> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new ImageGeneratorBuilder(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IImageGenerator), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}
